
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;
using Product.BLL.Interfaces;

namespace Product.BLL.Implementation;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;

    public EmailService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendEmailWithAttachmentAsync(
        string toEmail,
        string subject,
        string message,
        byte[] attachmentBytes,
        string attachmentFileName)
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
    }
}
