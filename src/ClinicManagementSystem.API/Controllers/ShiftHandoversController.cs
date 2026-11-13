using System.Security.Claims;
using Asp.Versioning;
using ClinicManagementSystem.Application.Common;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.Interfaces;
using ClinicManagementSystem.Application.ShiftHandovers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagementSystem.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/shift-handovers")]
[Tags("Shift Handovers (SBAR)")]
[Authorize(Roles = "Nurse,Doctor,Admin")]
public class ShiftHandoversController : ControllerBase
{
    private readonly ISender _sender;
    private readonly ICurrentUserService _currentUserService;

    public ShiftHandoversController(ISender sender, ICurrentUserService currentUserService)
    {
        _sender = sender;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    [EndpointSummary("Get list of SBAR shift handovers")]
    [ProducesResponseType(typeof(IEnumerable<ShiftHandoverResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] DateTime? date = null,
        [FromQuery] string? shiftType = null,
        CancellationToken ct = default)
    {
        var dtos = await _sender.Send(new GetShiftHandoversQuery(date, shiftType), ct);
        return Ok(dtos);
    }

    [HttpGet("latest")]
    [EndpointSummary("Get most recent shift handover")]
    [ProducesResponseType(typeof(ShiftHandoverResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> GetLatest(CancellationToken ct = default)
    {
        var item = await _sender.Send(new GetLatestShiftHandoverQuery(), ct);
        if (item == null)
        {
            return NoContent();
        }

        return Ok(item);
    }

    [HttpPost]
    [Authorize(Roles = "Nurse,Admin")]
    [EndpointSummary("Create a new SBAR shift handover note")]
    [ProducesResponseType(typeof(ShiftHandoverResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateShiftHandoverDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.ShiftType) || string.IsNullOrWhiteSpace(dto.Situation))
        {
            return BadRequest(new ProblemDetails { Title = "Validation Error", Detail = "ShiftType and Situation are required.", Status = 400 });
        }

        Guid authorId = _currentUserService.UserId ?? Guid.Empty;
        if (authorId == Guid.Empty)
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(claim, out var parsedClaim))
            {
                return Unauthorized(new ProblemDetails { Title = "Unauthorized", Detail = "Authenticated nurse identity is required.", Status = 401 });
            }
            authorId = parsedClaim;
        }

        var result = await _sender.Send(new CreateShiftHandoverCommand(
            authorId,
            dto.ShiftType,
            dto.HandoverTimestamp,
            dto.Situation,
            dto.Background,
            dto.Assessment,
            dto.Recommendation,
            dto.ActivePatientCount,
            dto.CriticalAlertsCount), ct);

        return result.Match<IActionResult>(
            onSuccess: created => CreatedAtAction(nameof(GetLatest), new { }, created),
            onFailure: (error, _) => BadRequest(new ProblemDetails { Title = "Handover Creation Failed", Detail = error, Status = 400 })
        );
    }
}
