using Asp.Versioning;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.UserRoles.Commands;
using ClinicManagementSystem.Application.UserRoles.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagementSystem.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/users")]
[Authorize(Roles = "Admin")]
public class UserRolesController : ControllerBase
{
    private readonly ISender _sender;

    public UserRolesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost("{userId:guid}/roles")]
    [EndpointSummary("Assign a role to a user")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AssignRole(Guid userId, [FromBody] string roleName)
    {
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

    [HttpGet("{userId:guid}/roles")]
    [EndpointSummary("Get roles for a specific user")]
    [ProducesResponseType(typeof(UserWithRolesDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserRoles(Guid userId)
    {
        var result = await _sender.Send(new GetUserRolesQuery(userId));
        return Ok(result.Value);
    }

    [HttpGet("with-roles")]
    [EndpointSummary("Get all users with their roles")]
    [ProducesResponseType(typeof(IEnumerable<UserWithRolesDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllUsersWithRoles()
    {
        var result = await _sender.Send(new GetAllUsersWithRolesQuery());
        return Ok(result.Value);
    }
}
