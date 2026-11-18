
namespace Product.DAL.Entities;

public class RefreshTokenEntity
{
    #region Properties

    public int Id { get; set; }

    public int UserId { get; set; }

    public string TokenHash { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime ExpiresAt { get; set; }

    public DateTime? RevokedAt { get; set; }

    public string? ReplacedByTokenHash { get; set; }

    public ApplicationUserEntity User { get; set; } = null!;

    #endregion
}
