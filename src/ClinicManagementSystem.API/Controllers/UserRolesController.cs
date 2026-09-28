using Asp.Versioning;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.UserRoles.Commands;
using ClinicManagementSystem.Application.UserRoles.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagementSystem.API.Controllers;

public record AssignRoleRequest(string RoleName);
public record ResetPasswordRequest(string NewPassword);

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/users")]
[Authorize]
public class UserRolesController : ControllerBase
{
    private readonly ISender _sender;

    public UserRolesController(ISender sender)
    {
        _sender = sender;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // STAFF DIRECTORY: CRUD
    // ─────────────────────────────────────────────────────────────────────────

    [HttpGet("with-roles")]
    [EndpointSummary("Get all users with their assigned roles and profile status")]
    [ProducesResponseType(typeof(IEnumerable<UserWithRolesDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllUsersWithRoles()
    {
        var result = await _sender.Send(new GetAllUsersWithRolesQuery());
        return Ok(result.Value);
    }

    [HttpGet("{userId:guid}/roles")]
    [EndpointSummary("Get roles for a specific user")]
    [ProducesResponseType(typeof(UserWithRolesDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserRoles(Guid userId)
    {
        var result = await _sender.Send(new GetUserRolesQuery(userId));
        return Ok(result.Value);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ATOMIC STAFF PROVISIONING (Admin creates staff with role in one step)
    // ─────────────────────────────────────────────────────────────────────────

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [EndpointSummary("Admin: atomically provision a new staff account with an assigned role")]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AdminCreateUser([FromBody] AdminCreateUserDto dto)
    {
        if (dto is null || string.IsNullOrWhiteSpace(dto.UserName) || string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
            return BadRequest(new ProblemDetails { Title = "Validation Error", Detail = "UserName, Email, and Password are required.", Status = 400 });

        var result = await _sender.Send(new AdminCreateUserCommand(
            dto.UserName, dto.Email, dto.Password,
            dto.FirstName, dto.LastName,
            dto.RoleName, dto.PhoneNumber));

        return result.Match<IActionResult>(
            onSuccess: newUserId => CreatedAtAction(nameof(GetUserRoles), new { userId = newUserId }, new { userId = newUserId }),
            onFailure: (error, errorCode) => errorCode switch
            {
                "duplicate_username" => Conflict(new ProblemDetails { Title = "Conflict", Detail = error, Status = 409 }),
                "duplicate_email" => Conflict(new ProblemDetails { Title = "Conflict", Detail = error, Status = 409 }),
                "role_not_found" => BadRequest(new ProblemDetails { Title = "Invalid Role", Detail = error, Status = 400 }),
                _ => BadRequest(new ProblemDetails { Title = "Error", Detail = error, Status = 400 })
            }
        );
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ROLE MANAGEMENT
    // ─────────────────────────────────────────────────────────────────────────

    [HttpPost("{userId:guid}/roles")]
    [Authorize(Roles = "Admin")]
    [EndpointSummary("Assign a role to a user")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AssignRole(Guid userId, [FromBody] AssignRoleRequest request)
    {
        var roleName = request?.RoleName;
        if (string.IsNullOrWhiteSpace(roleName))
            return BadRequest(new ProblemDetails { Title = "Validation Error", Detail = "RoleName is required", Status = 400 });

        var result = await _sender.Send(new AssignRoleCommand(userId, roleName));

        return result.Match<IActionResult>(
            onSuccess: _ => NoContent(),
            onFailure: (error, errorCode) => errorCode switch
            {
                "user_not_found" => NotFound(new ProblemDetails { Title = "Not Found", Detail = error, Status = 404 }),
                "role_not_found" => NotFound(new ProblemDetails { Title = "Not Found", Detail = error, Status = 404 }),
                "duplicate_role" => Conflict(new ProblemDetails { Title = "Conflict", Detail = error, Status = 409 }),
                _ => BadRequest(new ProblemDetails { Title = "Error", Detail = error, Status = 400 })
            }
        );
    }

    [HttpDelete("{userId:guid}/roles/{roleName}")]
    [Authorize(Roles = "Admin")]
    [EndpointSummary("Remove a role from a user")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveRole(Guid userId, string roleName)
    {
        var result = await _sender.Send(new RemoveRoleCommand(userId, roleName));

        return result.Match<IActionResult>(
            onSuccess: _ => NoContent(),
            onFailure: (error, errorCode) => NotFound(new ProblemDetails { Title = "Not Found", Detail = error, Status = 404 })
        );
    }

    // ─────────────────────────────────────────────────────────────────────────
    // CREDENTIAL MANAGEMENT
    // ─────────────────────────────────────────────────────────────────────────

    [HttpPost("{userId:guid}/reset-password")]
    [Authorize(Roles = "Admin")]
    [EndpointSummary("Admin: reset a staff member's password directly (bypass email flow)")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AdminResetPassword(Guid userId, [FromBody] ResetPasswordRequest request)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.NewPassword))
            return BadRequest(new ProblemDetails { Title = "Validation Error", Detail = "NewPassword is required.", Status = 400 });

        var result = await _sender.Send(new AdminResetPasswordCommand(userId, request.NewPassword));

        return result.Match<IActionResult>(
            onSuccess: _ => NoContent(),
            onFailure: (error, errorCode) => errorCode switch
            {
                "user_not_found" => NotFound(new ProblemDetails { Title = "Not Found", Detail = error, Status = 404 }),
                "password_too_short" => BadRequest(new ProblemDetails { Title = "Validation Error", Detail = error, Status = 400 }),
                _ => BadRequest(new ProblemDetails { Title = "Error", Detail = error, Status = 400 })
            }
        );
    }

    // ─────────────────────────────────────────────────────────────────────────
    // STATUS MANAGEMENT
    // ─────────────────────────────────────────────────────────────────────────

    [HttpPost("{userId:guid}/toggle-status")]
    [Authorize(Roles = "Admin")]
    [EndpointSummary("Toggle user active/inactive status")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ToggleUserStatus(Guid userId)
    {
        var result = await _sender.Send(new ToggleUserStatusCommand(userId));

        return result.Match<IActionResult>(
            onSuccess: newStatus => Ok(new { userId, isActive = newStatus }),
            onFailure: (error, errorCode) => NotFound(new ProblemDetails { Title = "Not Found", Detail = error, Status = 404 })
        );
    }
}
