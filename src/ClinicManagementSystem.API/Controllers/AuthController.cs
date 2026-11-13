using Asp.Versioning;
using ClinicManagementSystem.Application.Auth.Commands;
using ClinicManagementSystem.Application.Auth.Queries;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.Options;
using MediatR;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace ClinicManagementSystem.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth")]
public class AuthController : ControllerBase
{
    public const string RefreshTokenCookieName = "clinic_refresh_token";
    public const string XsrfCookieName = "XSRF-TOKEN";

    private readonly ISender _sender;
    private readonly IAntiforgery _antiforgery;
    private readonly IHostEnvironment _environment;
    private readonly JwtOptions _jwtOptions;

    public AuthController(
        ISender sender,
        IAntiforgery antiforgery,
        IHostEnvironment environment,
        IOptions<JwtOptions> jwtOptions)
    {
        _sender = sender;
        _antiforgery = antiforgery;
        _environment = environment;
        _jwtOptions = jwtOptions.Value;
    }

    [HttpGet("csrf")]
    [AllowAnonymous]
    [EndpointSummary("Issue CSRF/XSRF token cookies for browser clients")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult GetCsrfToken()
    {
        IssueXsrfCookie();
        return NoContent();
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting("RefreshPolicy")]
    [EndpointSummary("Register a new user")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterUserDto dto, CancellationToken ct)
    {
        var command = new RegisterUserCommand(dto.Username, dto.Email, dto.Password, dto.FirstName, dto.LastName, dto.PhoneNumber ?? "");
        var auth = await _sender.Send(command, ct);
        SetAuthCookies(auth.RefreshToken);
        return Ok(auth);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("LoginPolicy")]
    [EndpointSummary("Login and receive JWT and HttpOnly refresh cookie")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status423Locked)]
    public async Task<IActionResult> Login([FromBody] LoginDto dto, CancellationToken ct)
    {
        var command = new LoginCommand(dto.Username, dto.Password);
        var auth = await _sender.Send(command, ct);
        SetAuthCookies(auth.RefreshToken);
        return Ok(auth);
    }

    [HttpGet("me")]
    [Authorize]
    [EndpointSummary("Get currently authenticated user details")]
    [ProducesResponseType(typeof(UserResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCurrentUser(CancellationToken ct)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized(new ProblemDetails { Title = "Unauthorized", Detail = "User identity not found in token.", Status = 401 });

        var user = await _sender.Send(new GetCurrentUserQuery(userId), ct);
        if (user is null)
            return NotFound(new ProblemDetails { Title = "Not Found", Detail = "User not found", Status = 404 });

        return Ok(user);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting("RefreshPolicy")]
    [EndpointSummary("Refresh an expired JWT access token using HttpOnly cookie or body refresh token")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenDto? dto = null, CancellationToken ct = default)
    {
        var usingCookie = string.IsNullOrWhiteSpace(dto?.RefreshToken);
        var token = dto?.RefreshToken;

        if (usingCookie && Request.Cookies.TryGetValue(RefreshTokenCookieName, out var cookieToken))
        {
            // Browser cookie refresh path requires valid CSRF token when XSRF cookie was issued
            if (Request.Cookies.ContainsKey(XsrfCookieName))
            {
                try
                {
                    await _antiforgery.ValidateRequestAsync(HttpContext);
                }
                catch (AntiforgeryValidationException)
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
                    {
                        Title = "CSRF Validation Failed",
                        Detail = "Invalid or missing X-XSRF-TOKEN header.",
                        Status = StatusCodes.Status403Forbidden
                    });
                }
            }

            token = cookieToken;
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Unauthorized",
                Detail = "Refresh token is missing.",
                Status = StatusCodes.Status401Unauthorized
            });
        }

        var auth = await _sender.Send(new RefreshTokenCommand(token), ct);
        SetAuthCookies(auth.RefreshToken);
        return Ok(auth);
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    [EndpointSummary("Revoke refresh token and clear authentication cookies")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout([FromBody] LogoutDto? dto = null, CancellationToken ct = default)
    {
        var token = dto?.RefreshToken;
        if (string.IsNullOrWhiteSpace(token))
        {
            Request.Cookies.TryGetValue(RefreshTokenCookieName, out token);
        }

        if (!string.IsNullOrWhiteSpace(token))
        {
            await _sender.Send(new LogoutCommand(token), ct);
        }

        ClearAuthCookies();
        return NoContent();
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [EnableRateLimiting("RefreshPolicy")]
    [EndpointSummary("Request a password reset link")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto, CancellationToken ct)
    {
        var result = await _sender.Send(new ForgotPasswordCommand(dto.Email), ct);
        var devOnlyToken = _environment.IsDevelopment() ? result.Value : null;
        return Ok(new
        {
            message = "If the email exists, a password reset link has been dispatched.",
            resetToken = devOnlyToken,
            resetLink = !string.IsNullOrEmpty(devOnlyToken) ? $"/reset-password?token={devOnlyToken}" : null
        });
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    [EnableRateLimiting("RefreshPolicy")]
    [EndpointSummary("Reset password with a valid token")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto, CancellationToken ct)
    {
        var result = await _sender.Send(new ResetPasswordCommand(dto.Token, dto.NewPassword), ct);

        return result.Match<IActionResult>(
            onSuccess: _ => Ok(new { message = "Password reset successful." }),
            onFailure: (error, errorCode) => BadRequest(new ProblemDetails
            {
                Title = "Error",
                Detail = error,
                Status = StatusCodes.Status400BadRequest
            })
        );
    }

    private void SetAuthCookies(string refreshToken)
    {
        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            Response.Cookies.Append(RefreshTokenCookieName, refreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                Path = "/api",
                Expires = DateTimeOffset.UtcNow.AddDays(_jwtOptions.RefreshTokenExpiryDays),
                IsEssential = true
            });
        }

        IssueXsrfCookie();
    }

    private void IssueXsrfCookie()
    {
        var tokens = _antiforgery.GetAndStoreTokens(HttpContext);
        if (!string.IsNullOrWhiteSpace(tokens.RequestToken))
        {
            Response.Cookies.Append(XsrfCookieName, tokens.RequestToken, new CookieOptions
            {
                HttpOnly = false,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                Path = "/",
                IsEssential = true
            });
        }
    }

    private void ClearAuthCookies()
    {
        Response.Cookies.Delete(RefreshTokenCookieName, new CookieOptions { Path = "/api" });
        Response.Cookies.Delete(XsrfCookieName, new CookieOptions { Path = "/" });
    }
}
