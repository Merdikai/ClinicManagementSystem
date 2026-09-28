using System;
using System.Threading.Tasks;
using Asp.Versioning;
using ClinicManagementSystem.API.Constants;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.EmrAccess.Commands;
using ClinicManagementSystem.Application.EmrAccess.DTOs;
using ClinicManagementSystem.Application.EmrAccess.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClinicManagementSystem.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/emr-access")]
[Tags("EMR Access & HIPAA Audit")]
[Authorize]
[EnableRateLimiting(RateLimitingConstants.StaffPolicy)]
public class EmrAccessController : ControllerBase
{
    private readonly ISender _sender;

    public EmrAccessController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost("log")]
    [EndpointSummary("Log standard EMR chart access event (HIPAA compliant)")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    public async Task<IActionResult> LogAccess([FromBody] LogEmrAccessDto dto)
    {
        var id = await _sender.Send(new LogEmrAccessCommand(dto));
        return Ok(new { id, message = "EMR chart access successfully logged." });
    }

    [HttpPost("break-glass")]
    [Authorize(Roles = "Doctor,Nurse,Admin")]
    [EndpointSummary("Execute Break-Glass emergency EMR access protocol with clinical rationale")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BreakGlass([FromBody] BreakGlassRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Rationale))
            return BadRequest(new ProblemDetails { Title = "Invalid Override", Detail = "Clinical rationale is mandatory for break-glass emergency override." });

        var id = await _sender.Send(new BreakGlassEmergencyCommand(dto));
        return Ok(new { id, message = "🚨 EMERGENCY OVERRIDE LOGGED: Break-Glass protocol active." });
    }

    [HttpGet("audit-logs")]
    [Authorize(Roles = "Admin")]
    [EndpointSummary("Retrieve HIPAA EMR security access and break-glass audit logs")]
    [ProducesResponseType(typeof(PagedResponse<EmrAccessAuditLogDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAuditLogs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? patientId = null,
        [FromQuery] bool? emergencyOnly = null)
    {
        var result = await _sender.Send(new GetEmrAccessAuditLogsQuery(page, pageSize, patientId, emergencyOnly));
        return Ok(result);
    }
}
