namespace Product.BLL.Interfaces.ReportSevrice;

public interface IEmailService
{
    Task SendEmailWithAttachmentAsync(
        string toEmail,
        string subject,
        string message,
        byte[] attachmentBytes,
        string attachmentFileName);
}
