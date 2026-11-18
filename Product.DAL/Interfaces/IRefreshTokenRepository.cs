
using Product.DAL.Entities;

namespace Product.DAL.Interfaces;

public interface IRefreshTokenRepository
{
    Task CreateAsync(RefreshTokenEntity token);

    Task<RefreshTokenEntity?> GetByHashAsync(string tokenHash);

    Task RotateAsync(
        RefreshTokenEntity existing,
        RefreshTokenEntity replacement);

    Task UpdateAsync(RefreshTokenEntity token);
}
