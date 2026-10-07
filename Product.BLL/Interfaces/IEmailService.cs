
namespace Product.BLL.Interfaces;

public interface IEmailService
{
    Task SendEmailWithAttachmentAsync(
        string toEmail,
        string subject,
        string message,
        byte[] attachmentBytes,
        string attachmentFileName);
}
