
using Microsoft.EntityFrameworkCore;
using Product.DAL.Context;
using Product.DAL.Entities;
using Product.DAL.Interfaces;

namespace Product.DAL.Implementation.Auth;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly AppDbContext _context;

    #region Ctor

    public RefreshTokenRepository(AppDbContext context)
    {
        _context = context;
    }

    #endregion

    #region Create

    public async Task CreateAsync(RefreshTokenEntity token)
    {
        await _context.RefreshTokens.AddAsync(token);

        await _context.SaveChangesAsync();
    }

    #endregion

    #region Get

    public async Task<RefreshTokenEntity?> GetByHashAsync(
        string tokenHash)
    {
        return await _context.RefreshTokens
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.TokenHash == tokenHash);
    }

    #endregion

    #region Update

    public async Task UpdateAsync(RefreshTokenEntity token)
    {
        await _context.SaveChangesAsync();
    }

    public async Task RotateAsync(
     RefreshTokenEntity existing,
     RefreshTokenEntity replacement)
    {
        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        try
        {
            await _context.RefreshTokens.AddAsync(replacement);

            await _context.SaveChangesAsync();

            existing.RevokedAt = DateTime.UtcNow;
            existing.ReplacedByTokenHash = replacement.TokenHash;

            await _context.SaveChangesAsync();

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    #endregion
}
