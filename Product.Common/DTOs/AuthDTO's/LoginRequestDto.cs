
using System.ComponentModel.DataAnnotations;

namespace Product.Common.DTOs.Auth;

public class LoginRequestDto
{
    #region Properties

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

    #endregion
}
