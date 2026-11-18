
using System.ComponentModel.DataAnnotations;

namespace Product.Common.DTOs.Auth;

public class RegisterRequestDto
{
    #region Properties

    [Required]
    [StringLength(150, MinimumLength = 2)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(128, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;

    [Phone]
    [StringLength(15)]
    public string? PhoneNumber { get; set; }

    #endregion
}
