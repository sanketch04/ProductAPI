
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Product.BLL.Interfaces;
using Product.BLL.Models;
using Product.DAL.Entities;
using Product.DAL.Interfaces;

namespace Product.BLL.Services;

public class RefreshTokenService : IRefreshTokenService
{
    private readonly IRefreshTokenRepository _repository;
    private readonly JwtSettings _settings;

    #region Ctor

    public RefreshTokenService(
        IRefreshTokenRepository repository,
        IOptions<JwtSettings> options)
    {
        _repository = repository;
        _settings = options.Value;
    }

    #endregion

    #region Create

    public async Task<(string Token, string Hash)> CreateAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var bytes = RandomNumberGenerator.GetBytes(64);

        var token = Convert.ToBase64String(bytes);

        var hash = HashToken(token);

        var entity = new RefreshTokenEntity
        {
            UserId = userId,
            TokenHash = hash,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(
                _settings.RefreshTokenExpiryDays)
        };

        await _repository.CreateAsync(entity);

        return (token, hash);
    }

    #endregion

    #region Rotate


    public async Task<RefreshTokenRotationResult?> RotateAsync(
        string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken) ||
            refreshToken.Length > 200)
            return null;

        var hash = HashToken(refreshToken);

        var existing = await _repository.GetByHashAsync(hash);

        if (existing == null ||
            existing.RevokedAt != null ||
            existing.ExpiresAt <= DateTime.UtcNow ||
            !existing.User.IsActive)
        {
            return null;
        }

        var replacementToken = Convert.ToBase64String(
            RandomNumberGenerator.GetBytes(64));

        var replacementHash = HashToken(replacementToken);

        var replacement = new RefreshTokenEntity
        {
            UserId = existing.UserId,
            TokenHash = replacementHash,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(
                _settings.RefreshTokenExpiryDays)
        };

        await _repository.RotateAsync(existing, replacement);

        return new RefreshTokenRotationResult
        {
            Token = replacementToken,
            User = existing.User
        };
    }


    #endregion

    #region Revoke

   

    #endregion

    #region Helpers

    private static string HashToken(string token)
    {
        return Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }

    #region Create

    public async Task<(string Token, string Hash)> CreateAsync(
        int userId)
    {
        // Generate cryptographically secure random bytes
        var bytes = RandomNumberGenerator.GetBytes(64);

        // Convert bytes into a readable token
        var token = Convert.ToBase64String(bytes);

        // Generate SHA256 hash of the token
        var hash = HashToken(token);

        // Create refresh token entity
        var entity = new RefreshTokenEntity
        {
            UserId = userId,
            TokenHash = hash,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(
                _settings.RefreshTokenExpiryDays)
        };

        // Save token hash in database
        await _repository.CreateAsync(entity);

        // Return original token and its hash
        return (token, hash);
    }

    #endregion

    public async Task RevokeAsync(
       string refreshToken,
       CancellationToken cancellationToken = default)
    {
        var hash = HashToken(refreshToken);

        var existing = await _repository.GetByHashAsync(hash);

        if (existing == null || existing.RevokedAt != null)
            return;

        existing.RevokedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(existing);
    }

    #endregion
}
