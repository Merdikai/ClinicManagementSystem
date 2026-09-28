using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Asp.Versioning;
using ClinicManagementSystem.API.Constants;
using ClinicManagementSystem.Application.DoctorDelegations.Commands;
using ClinicManagementSystem.Application.DoctorDelegations.DTOs;
using ClinicManagementSystem.Application.DoctorDelegations.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClinicManagementSystem.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/doctor-delegations")]
[Tags("Doctor Coverage & Handover Delegations")]
[Authorize]
[EnableRateLimiting(RateLimitingConstants.StaffPolicy)]
public class DoctorDelegationsController : ControllerBase
{
    private readonly ISender _sender;

    public DoctorDelegationsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("active")]
    [EndpointSummary("Get all currently active doctor coverage delegations (accessible to all authenticated staff)")]
    [ProducesResponseType(typeof(List<DoctorDelegationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActiveDelegations()
    {
        var result = await _sender.Send(new GetActiveDoctorDelegationsQuery());
        return Ok(result);
    }

    [HttpGet("my-delegations")]
    [EndpointSummary("Get personal delegations where doctor is either delegator or covering physician")]
    [ProducesResponseType(typeof(List<DoctorDelegationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyDelegations()
    {
        var result = await _sender.Send(new GetMyDoctorDelegationsQuery());
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Doctor,Admin,Nurse")]
    [EndpointSummary("Create and authorize a new doctor coverage delegation")]
    [ProducesResponseType(typeof(DoctorDelegationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateDelegation([FromBody] CreateDoctorDelegationDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.CoveringDoctorName))
            return BadRequest(new ProblemDetails { Title = "Validation Error", Detail = "Covering doctor name is required." });

        if (dto.EndDateUtc <= dto.StartDateUtc)
            return BadRequest(new ProblemDetails { Title = "Validation Error", Detail = "End date must be after start date." });

        var result = await _sender.Send(new CreateDoctorDelegationCommand(dto));
        return Ok(result);
    }

    [HttpPost("{id:guid}/revoke")]
    [Authorize(Roles = "Doctor,Admin,Nurse")]
    [EndpointSummary("Revoke an active doctor coverage delegation")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokeDelegation(Guid id)
    {
        var result = await _sender.Send(new RevokeDoctorDelegationCommand(id));
        if (!result)
            return NotFound(new ProblemDetails { Title = "Not Found", Detail = "Delegation not found or already revoked." });

        return Ok(result);
    }
}
