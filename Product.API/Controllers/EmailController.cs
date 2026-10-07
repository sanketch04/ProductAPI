
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Product.API.Models;
using Product.BLL.Interfaces;
using System.Text;

namespace Product.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmailController : ControllerBase
{
    private readonly IEmailService _emailService;

    public EmailController(IEmailService emailService)
    {
        _emailService = emailService;
    }

    [HttpPost("send-with-pdf")]
    [Consumes("multipart/form-data")]
    [EnableRateLimiting("EmailRateLimit")]
    public async Task<IActionResult> SendWithPdf([FromForm] SendEmailRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ToEmail) ||
            !System.Net.Mail.MailAddress.TryCreate(
                request.ToEmail, out var address) ||
            !string.Equals(
                address.Address,
                request.ToEmail,
                StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("Please provide a valid email address.");
        }

        if (string.IsNullOrWhiteSpace(request.Subject) ||
            string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest("Subject and message are required.");
        }

        var pdfFile = request.PdfFile;

        if (pdfFile == null || pdfFile.Length == 0)
        {
            return BadRequest("Please upload a PDF file.");
        }

        if (pdfFile.Length > 5 * 1024 * 1024)
        {
            return BadRequest("PDF must be 5 MB or smaller.");
        }

        if (!string.Equals(
            Path.GetExtension(pdfFile.FileName),
            ".pdf",
            StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("Only PDF files are allowed.");
        }

        await using var stream = new MemoryStream();
        await pdfFile.CopyToAsync(stream);

        var pdfBytes = stream.ToArray();

        if (pdfBytes.Length < 5 ||
            Encoding.ASCII.GetString(pdfBytes, 0, 5) != "%PDF-")
        {
            return BadRequest("The uploaded file is not a valid PDF.");
        }

        try
        {
            await _emailService.SendEmailWithAttachmentAsync(
                request.ToEmail,
                request.Subject,
                request.Message,
                pdfBytes,
                "ProductReport.pdf");

            return Ok(new
            {
                message = "Email sent successfully."
            });
        }
        catch (Exception ex) when (
            ex is MailKit.Net.Smtp.SmtpCommandException ||
            ex is MailKit.Net.Smtp.SmtpProtocolException ||
            ex is MailKit.ServiceNotConnectedException ||
            ex is MailKit.ServiceNotAuthenticatedException)
        {
            return StatusCode(502, new
            {
                message = "Unable to send email. Check SMTP configuration."
            });
        }
    }
}
