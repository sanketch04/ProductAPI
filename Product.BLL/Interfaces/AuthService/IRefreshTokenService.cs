
using Product.BLL.Models;

namespace Product.BLL.Interfaces;

public interface IRefreshTokenService
{
    Task<(string Token, string Hash)> CreateAsync(int userId);

    Task<RefreshTokenRotationResult?> RotateAsync(string refreshToken);

    Task RevokeAsync(string refreshToken, CancellationToken cancellationToken = default);
}
