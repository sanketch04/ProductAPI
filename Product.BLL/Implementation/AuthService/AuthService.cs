
using System.Text;
using Microsoft.Extensions.Logging;
using Product.BLL.Interfaces;
using Product.Common.Constants;
using Product.Common.DTOs.Auth;
using Product.DAL.Entities;
using Product.DAL.Interfaces;

namespace Product.BLL.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtService _jwtService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly ILogger<AuthService> _logger;

    #region Ctor

    public AuthService(
        IUserRepository userRepository,
        IJwtService jwtService,
        IRefreshTokenService refreshTokenService,
        ILogger<AuthService> logger)
    {
        _userRepository = userRepository;
        _jwtService = jwtService;
        _refreshTokenService = refreshTokenService;
        _logger = logger;
    }

    #endregion

    #region Register

    public async Task<AuthResponseDto> RegisterAsync(
        RegisterRequestDto request)
    {
        var email = request.Email.Trim();
        var normalizedEmail = email.ToUpperInvariant();

        ValidatePassword(request.Password);

        if (await _userRepository.EmailExistsAsync(normalizedEmail))
            throw new InvalidOperationException(
                "Email is already registered.");

        var user = new ApplicationUserEntity
        {
            FullName = request.FullName.Trim(),
            Email = email,
            NormalizedEmail = normalizedEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(
                request.Password, workFactor: 12),
            PhoneNumber = request.PhoneNumber?.Trim(),
            Role = AppRoles.User,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var createdUser =
            await _userRepository.CreateAsync(user);

        return await CreateAuthResponseAsync(createdUser);
    }

    #endregion

    #region Login

    public async Task<AuthResponseDto> LoginAsync(
        LoginRequestDto request)
    {
        ValidatePassword(request.Password);

        var normalizedEmail =
            request.Email.Trim().ToUpperInvariant();

        var user = await _userRepository.GetByEmailAsync(
            normalizedEmail);

        if (user == null ||
            !BCrypt.Net.BCrypt.Verify(
                request.Password, user.PasswordHash))
        {
            _logger.LogWarning(
                "Failed login attempt for supplied email.");

            throw new UnauthorizedAccessException(
                "Invalid email or password.");
        }

        if (!user.IsActive)
        {
            throw new UnauthorizedAccessException(
                "Your account is inactive.");
        }

        user.LastLoginAt = DateTime.UtcNow;

        await _userRepository.UpdateAsync(user);

        return await CreateAuthResponseAsync(user);
    }

    #endregion

    #region Refresh Token

    public async Task<AuthResponseDto?> RefreshAsync(
        string refreshToken)
    {
        var result = await _refreshTokenService.RotateAsync(
            refreshToken);

        if (result == null)
            return null;

        return await CreateAuthResponseAsync(
            result.User,
            result.Token);
    }

    #endregion

    #region Logout

    public async Task RevokeAsync(string refreshToken)
    {
        await _refreshTokenService.RevokeAsync(refreshToken);
    }

    #endregion

    #region Helpers

    private async Task<AuthResponseDto> CreateAuthResponseAsync(
        ApplicationUserEntity user,
        string? existingRefreshToken = null)
    {
        var accessToken =
            _jwtService.GenerateAccessToken(user);

        var refreshToken = existingRefreshToken;

        if (string.IsNullOrEmpty(refreshToken))
        {
            var tokenResult =
                await _refreshTokenService.CreateAsync(user.Id);

            refreshToken = tokenResult.Token;
        }

        return new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            TokenType = "Bearer",
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role
        };
    }

    private static void ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password) ||
            Encoding.UTF8.GetByteCount(password) > 72)
        {
            throw new ArgumentException(
                "Password must not exceed 72 UTF-8 bytes.");
        }
    }

    #endregion
}
