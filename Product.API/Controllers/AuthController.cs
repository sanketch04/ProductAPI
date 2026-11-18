
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Product.BLL.Interfaces;
using Product.Common.DTOs.Auth;

namespace Product.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    #region Ctor

    public AuthController(IAuthService authService,ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    #endregion

    #region Register

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
    {
        try
        {
            var result = await _authService.RegisterAsync(request);

            return CreatedAtAction(
                nameof(Register),
                new { id = result.UserId },
                result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    #endregion

    #region Login

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequestDto request)
    {
        try
        {
            var result = await _authService.LoginAsync(request);

            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    #endregion

    #region Refresh Token

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshTokenRequestDto request)
    {
        var result = await _authService.RefreshAsync(
            request.RefreshToken);

        if (result == null)
        {
            return Unauthorized(new
            {
                message = "Invalid or expired refresh token."
            });
        }

        return Ok(result);
    }

    #endregion

    #region Logout

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(
        [FromBody] RefreshTokenRequestDto request)
    {
        await _authService.RevokeAsync(request.RefreshToken);

        return Ok(new
        {
            message = "Refresh token revoked successfully."
        });
    }

    #endregion

}
