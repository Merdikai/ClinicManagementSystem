using System.Collections.Generic;
using System.Threading.Tasks;
using Asp.Versioning;
using ClinicManagementSystem.API.Constants;
using ClinicManagementSystem.API.Filters;
using ClinicManagementSystem.Application.Permissions.Commands;
using ClinicManagementSystem.Application.Permissions.DTOs;
using ClinicManagementSystem.Application.Permissions.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClinicManagementSystem.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/permissions")]
[Tags("Permissions & PBAC Matrix")]
[Authorize(Roles = "Admin")]
[EnableRateLimiting(RateLimitingConstants.StaffPolicy)]
public class PermissionsController : ControllerBase
{
    private readonly ISender _sender;

    public PermissionsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [ETagFilter]
    [EndpointSummary("Get fine-grained PBAC permissions matrix for all or a specific role")]
    [ProducesResponseType(typeof(List<RolePermissionMatrixDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRolePermissions([FromQuery] string? roleName = null)
    {
        var result = await _sender.Send(new GetRolePermissionsQuery(roleName));
        return Ok(result);
    }

    [HttpPut]
    [AuditAction("RolePermission", "Updated")]
    [EndpointSummary("Update granted permissions for a specific role")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateRolePermissions([FromBody] UpdateRolePermissionsDto dto)
    {
        var result = await _sender.Send(new UpdateRolePermissionsCommand(dto.RoleName, dto.Permissions));
        return Ok(result);
    }
}
