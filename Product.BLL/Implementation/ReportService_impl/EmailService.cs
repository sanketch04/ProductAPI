using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using Product.BLL.Interfaces.ReportSevrice;
using System.Diagnostics;

namespace Product.BLL.Implementation.ReportService_impl;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(
        IConfiguration configuration,
        ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendEmailWithAttachmentAsync(
        string toEmail,
        string subject,
        string message,
        byte[] attachmentBytes,
        string attachmentFileName)
    {
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation(
            "Sending email to {Recipient} with subject {Subject} and attachment {AttachmentName} ({AttachmentSizeBytes} bytes)",
            toEmail, subject, attachmentFileName, attachmentBytes.Length);

        try
        {
            var emailSettings =
                _configuration.GetSection("EmailSettings");

            var host = emailSettings["Host"]
                ?? throw new InvalidOperationException(
                    "SMTP host is not configured.");

            var port = int.Parse(
                emailSettings["Port"]
                ?? throw new InvalidOperationException(
                    "SMTP port is not configured."));

            var senderEmail = emailSettings["SenderEmail"]
                ?? throw new InvalidOperationException(
                    "Sender email is not configured.");

            var senderName = emailSettings["SenderName"]
                ?? "Product Management";

            var password = emailSettings["Password"]
                ?? throw new InvalidOperationException(
                    "SMTP password is not configured.");

            var email = new MimeMessage();

            email.From.Add(new MailboxAddress(senderName, senderEmail));

            email.To.Add(MailboxAddress.Parse(toEmail));
            email.Subject = subject;

            var body = new BodyBuilder
            {
                TextBody = message
            };

            body.Attachments.Add(
                attachmentFileName,
                attachmentBytes);

            email.Body = body.ToMessageBody();

            using var smtpClient = new SmtpClient();

            _logger.LogInformation(
                "Connecting to SMTP server {SmtpHost}:{SmtpPort}", host, port);

            await smtpClient.ConnectAsync(
                host,
                port,
                SecureSocketOptions.StartTls);

            try
            {
                await smtpClient.AuthenticateAsync(
                    senderEmail,
                    password);

                await smtpClient.SendAsync(email);
            }
            finally
            {
                if (smtpClient.IsConnected)
                {
                    await smtpClient.DisconnectAsync(true);
                }
            }

            _logger.LogInformation(
                "Email sent to {Recipient} in {ElapsedMs} ms",
                toEmail, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Email sending failed for {Recipient} after {ElapsedMs} ms",
                toEmail, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}