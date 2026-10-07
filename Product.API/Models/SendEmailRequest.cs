
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Product.API.Models;

public class SendEmailRequest
{
    [Required]
    [EmailAddress]
    public string ToEmail { get; set; } = string.Empty;

    [Required]
    public string Subject { get; set; } = string.Empty;

    [Required]
    public string Message { get; set; } = string.Empty;

    [Required]
    public IFormFile PdfFile { get; set; } = null!;
}
