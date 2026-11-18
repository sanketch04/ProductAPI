
namespace Product.BLL.Models;

public class JwtSettings
{
    #region Properties

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public string SecretKey { get; set; } = string.Empty;

    public int AccessTokenExpiryMinutes { get; set; } = 15;

    public int RefreshTokenExpiryDays { get; set; } = 7;

    #endregion
}
