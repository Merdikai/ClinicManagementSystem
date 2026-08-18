using Asp.Versioning;
using ClinicManagementSystem.Application.Auth.Commands;
using ClinicManagementSystem.Application.Auth.Queries;
using ClinicManagementSystem.Application.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ClinicManagementSystem.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth")]
public class AuthController : ControllerBase
{
    private readonly ISender _sender;

    public AuthController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost("register")]
    [EndpointSummary("Register a new user")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterUserDto dto)
    {
        var command = new RegisterUserCommand(dto.Username, dto.Email, dto.Password, dto.FirstName, dto.LastName, dto.PhoneNumber ?? "");
        var auth = await _sender.Send(command);
        return Ok(auth);
    }

    [HttpPost("login")]
    [EndpointSummary("Login and receive JWT and refresh tokens")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var command = new LoginCommand(dto.Username, dto.Password);
        var auth = await _sender.Send(command);
        return Ok(auth);
    }

    [HttpGet("me")]
    [Authorize]
    [EndpointSummary("Get currently authenticated user details")]
    [ProducesResponseType(typeof(UserResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var user = await _sender.Send(new GetCurrentUserQuery(userId));
        if (user is null)
            return NotFound(new ProblemDetails { Title = "Not Found", Detail = "User not found", Status = 404 });

        return Ok(user);
    }

    [HttpPost("refresh")]
    [EndpointSummary("Refresh an expired JWT access token using a valid refresh token")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenDto dto)
    {
        var command = new RefreshTokenCommand(dto.RefreshToken);
        var auth = await _sender.Send(command);
        return Ok(auth);
    }

    [HttpPost("logout")]
    [Authorize]
    [EndpointSummary("Revoke a refresh token on user logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Logout([FromBody] LogoutDto dto)
    {
        var command = new LogoutCommand(dto.RefreshToken);
        await _sender.Send(command);
        return NoContent();
    }

    [HttpPost("forgot-password")]
    [EndpointSummary("Request a password reset link")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
    {
        await _sender.Send(new ForgotPasswordCommand(dto.Email));
        return Ok(new { message = "If the email exists, a reset link has been sent." });
    }

    [HttpPost("reset-password")]
    [EndpointSummary("Reset password with a valid token")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
    {
        var result = await _sender.Send(new ResetPasswordCommand(dto.Token, dto.NewPassword));

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
}
