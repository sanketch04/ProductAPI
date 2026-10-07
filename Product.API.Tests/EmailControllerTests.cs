using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Product.API.Controllers;
using Product.API.Models;
using Product.BLL.Interfaces;
using Xunit;

namespace Product.API.Tests.Controllers;

public class EmailControllerTests
{
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly EmailController _controller;

    public EmailControllerTests()
    {
        // xUnit creates a NEW instance of this class for every test,
        // so each test gets a fresh mock and controller (no shared state).
        _emailServiceMock = new Mock<IEmailService>();
        _controller = new EmailController(_emailServiceMock.Object);
    }

    // ---------- Helpers ----------

    private static IFormFile CreatePdfFile(
        byte[]? content = null, string fileName = "report.pdf")
    {
        content ??= Encoding.ASCII.GetBytes("%PDF-1.4 fake pdf content");
        var stream = new MemoryStream(content);
        return new FormFile(stream, 0, content.Length, "PdfFile", fileName);
    }

    private static SendEmailRequest CreateValidRequest() => new()
    {
        ToEmail = "test@example.com",
        Subject = "Monthly report",
        Message = "Please find the attached report.",
        PdfFile = CreatePdfFile()
    };

    // Reads the "message" property from an anonymous object like new { message = "..." }
    private static string? GetMessage(object? value) =>
        value?.GetType().GetProperty("message")?.GetValue(value) as string;

    // ---------- 1. Email validation ----------

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-an-email")]
    [InlineData("John <john@example.com>")]   // display-name format is rejected
    public async Task SendWithPdf_InvalidEmail_ReturnsBadRequest(string email)
    {
        // Arrange
        var request = CreateValidRequest();
        request.ToEmail = email;

        // Act
        var result = await _controller.SendWithPdf(request);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);

        // Assert
        Assert.Equal("Please provide a valid email address.", badRequest.Value);
        VerifyEmailNeverSent();
    }

    // ---------- 2. Subject and message ----------

    [Theory]
    [InlineData("", "Hello")]
    [InlineData("Subject", "")]
    [InlineData(" ", " ")]
    public async Task SendWithPdf_MissingSubjectOrMessage_ReturnsBadRequest(
        string subject, string message)
    {
        var request = CreateValidRequest();
        request.Subject = subject;
        request.Message = message;

        var result = await _controller.SendWithPdf(request);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Subject and message are required.", badRequest.Value);
        VerifyEmailNeverSent();
    }

    // ---------- 3. PDF missing or empty ----------

    [Fact]
    public async Task SendWithPdf_NullPdf_ReturnsBadRequest()
    {
        //Arrange
        var request = CreateValidRequest();
        request.PdfFile = null;

        //Act
        var result = await _controller.SendWithPdf(request);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);

        //Assert
        Assert.Equal("Please upload a PDF file.", badRequest.Value);
        VerifyEmailNeverSent();
    }

    [Fact]
    public async Task SendWithPdf_EmptyPdf_ReturnsBadRequest()
    {
        var request = CreateValidRequest();
        request.PdfFile = CreatePdfFile(content: Array.Empty<byte>());

        var result = await _controller.SendWithPdf(request);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Please upload a PDF file.", badRequest.Value);
        VerifyEmailNeverSent();
    }

    // ---------- 4. File too large ----------

    [Fact]
    public async Task SendWithPdf_PdfLargerThan5MB_ReturnsBadRequest()
    {
        // Mock IFormFile so we don't allocate 5 MB of memory
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.Length).Returns(5 * 1024 * 1024 + 1);
        fileMock.Setup(f => f.FileName).Returns("big.pdf");

        var request = CreateValidRequest();
        request.PdfFile = fileMock.Object;

        var result = await _controller.SendWithPdf(request);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("PDF must be 5 MB or smaller.", badRequest.Value);
        VerifyEmailNeverSent();
    }

    // ---------- 5. Wrong extension ----------

    [Theory]
    [InlineData("report.txt")]
    [InlineData("report.exe")]
    [InlineData("report")]
    public async Task SendWithPdf_NonPdfExtension_ReturnsBadRequest(string fileName)
    {
        var request = CreateValidRequest();
        request.PdfFile = CreatePdfFile(fileName: fileName);

        var result = await _controller.SendWithPdf(request);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Only PDF files are allowed.", badRequest.Value);
        VerifyEmailNeverSent();
    }

    // ---------- 6. Fake PDF (bad content) ----------

    [Fact]
    public async Task SendWithPdf_FileWithoutPdfHeader_ReturnsBadRequest()
    {
        var request = CreateValidRequest();
        request.PdfFile = CreatePdfFile(
            content: Encoding.ASCII.GetBytes("this is just text, not a pdf"));

        var result = await _controller.SendWithPdf(request);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("The uploaded file is not a valid PDF.", badRequest.Value);
        VerifyEmailNeverSent();
    }

    [Fact]
    public async Task SendWithPdf_FileShorterThan5Bytes_ReturnsBadRequest()
    {
        var request = CreateValidRequest();
        request.PdfFile = CreatePdfFile(content: new byte[] { 1, 2, 3 });

        var result = await _controller.SendWithPdf(request);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("The uploaded file is not a valid PDF.", badRequest.Value);
    }

    // ---------- 7. Happy path ----------

    [Fact]
    public async Task SendWithPdf_ValidRequest_ReturnsOkAndSendsEmail()
    {
        // Arrange
        var request = CreateValidRequest();

        _emailServiceMock
            .Setup(s => s.SendEmailWithAttachmentAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<byte[]>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.SendWithPdf(request);

        // Assert: response
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("Email sent successfully.", GetMessage(ok.Value));

        // Assert: the service was called exactly once with the right values
        _emailServiceMock.Verify(s => s.SendEmailWithAttachmentAsync(
            "test@example.com",
            "Monthly report",
            "Please find the attached report.",
            It.Is<byte[]>(b => b.Length > 0 && b[0] == (byte)'%'),
            "ProductReport.pdf"),
            Times.Once);
    }

    // ---------- 8. SMTP failure ----------

    [Fact]
    public async Task SendWithPdf_SmtpFailure_Returns502()
    {
        var request = CreateValidRequest();

        _emailServiceMock
            .Setup(s => s.SendEmailWithAttachmentAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<byte[]>(), It.IsAny<string>()))
            .ThrowsAsync(new MailKit.ServiceNotConnectedException("not connected"));

        var result = await _controller.SendWithPdf(request);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(502, objectResult.StatusCode);
        Assert.Equal("Unable to send email. Check SMTP configuration.",
            GetMessage(objectResult.Value));
    }

    // ---------- 9. Unexpected exception is NOT swallowed ----------

    [Fact]
    public async Task SendWithPdf_UnexpectedException_IsNotCaught()
    {
        var request = CreateValidRequest();

        _emailServiceMock
            .Setup(s => s.SendEmailWithAttachmentAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<byte[]>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _controller.SendWithPdf(request));
    }

    // ---------- Shared verification ----------

    private void VerifyEmailNeverSent() =>
        _emailServiceMock.Verify(s => s.SendEmailWithAttachmentAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<byte[]>(), It.IsAny<string>()),
            Times.Never);
}