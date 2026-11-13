using System.Security.Claims;
using Asp.Versioning;
using ClinicManagementSystem.Application.Common;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.Interfaces;
using ClinicManagementSystem.Application.NursingProcedures;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagementSystem.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/nursing-procedures")]
[Tags("Nursing Procedures")]
[Authorize(Roles = "Nurse,Doctor,Admin")]
public class NursingProceduresController : ControllerBase
{
    private readonly ISender _sender;
    private readonly ICurrentUserService _currentUserService;

    public NursingProceduresController(ISender sender, ICurrentUserService currentUserService)
    {
        _sender = sender;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    [EndpointSummary("Get list of nursing procedures with optional filters")]
    [ProducesResponseType(typeof(IEnumerable<NursingProcedureResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? patientId = null,
        [FromQuery] Guid? appointmentId = null,
        [FromQuery] DateTime? date = null,
        [FromQuery] string? status = null,
        CancellationToken ct = default)
    {
        var dtos = await _sender.Send(new GetNursingProceduresQuery(patientId, appointmentId, date, status), ct);
        return Ok(dtos);
    }

    [HttpGet("{id:guid}")]
    [EndpointSummary("Get a nursing procedure by ID")]
    [ProducesResponseType(typeof(NursingProcedureResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct = default)
    {
        var dto = await _sender.Send(new GetNursingProcedureByIdQuery(id), ct);
        if (dto == null)
        {
            return NotFound(new ProblemDetails { Title = "Not Found", Detail = $"Nursing procedure {id} not found.", Status = 404 });
        }

        return Ok(dto);
    }

    [HttpPost]
    [Authorize(Roles = "Nurse,Doctor,Admin")]
    [EndpointSummary("Record a direct nursing procedure performed immediately")]
    [ProducesResponseType(typeof(NursingProcedureResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateNursingProcedureDto dto, CancellationToken ct = default)
    {
        if (dto.PatientId == Guid.Empty)
        {
            return BadRequest(new ProblemDetails { Title = "Invalid Patient", Detail = "PatientId is required.", Status = 400 });
        }

        Guid performerId = _currentUserService.UserId ?? Guid.Empty;
        if (performerId == Guid.Empty)
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(claim, out var parsedClaim))
            {
                return Unauthorized(new ProblemDetails { Title = "Unauthorized", Detail = "Authenticated clinician identity is required.", Status = 401 });
            }
            performerId = parsedClaim;
        }

        var result = await _sender.Send(new CreateNursingProcedureCommand(
            dto.PatientId,
            dto.AppointmentId,
            performerId,
            dto.ProcedureType,
            dto.BodySite,
            dto.Notes,
            dto.PatientTolerance,
            dto.SuppliesUsed,
            dto.PerformedAt), ct);

        return result.Match<IActionResult>(
            onSuccess: created => CreatedAtAction(nameof(GetById), new { id = created.Id }, created),
            onFailure: (error, _) => BadRequest(new ProblemDetails { Title = "Procedure Creation Failed", Detail = error, Status = 400 })
        );
    }

    [HttpPost("orders")]
    [Authorize(Roles = "Doctor,Admin")]
    [EndpointSummary("Place an in-clinic nursing procedure order (by Doctor or Clinician)")]
    [ProducesResponseType(typeof(NursingProcedureResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> OrderProcedure([FromBody] OrderNursingProcedureRequestDto dto, CancellationToken ct = default)
    {
        if (dto.PatientId == Guid.Empty)
        {
            return BadRequest(new ProblemDetails { Title = "Invalid Patient", Detail = "PatientId is required.", Status = 400 });
        }

        Guid doctorId = _currentUserService.UserId ?? Guid.Empty;
        if (doctorId == Guid.Empty && Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var parsedClaim))
        {
            doctorId = parsedClaim;
        }

        var result = await _sender.Send(new OrderNursingProcedureCommand(
            dto.PatientId,
            dto.AppointmentId,
            doctorId != Guid.Empty ? doctorId : null,
            dto.ProcedureType,
            dto.BodySite,
            dto.Notes,
            dto.Priority,
            dto.Price), ct);

        return result.Match<IActionResult>(
            onSuccess: created => CreatedAtAction(nameof(GetById), new { id = created.Id }, created),
            onFailure: (error, _) => BadRequest(new ProblemDetails { Title = "Order Creation Failed", Detail = error, Status = 400 })
        );
    }

    [HttpPost("{id:guid}/complete")]
    [Authorize(Roles = "Nurse,Admin")]
    [EndpointSummary("Fulfill and complete an ordered nursing procedure (by Nurse)")]
    [ProducesResponseType(typeof(NursingProcedureResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CompleteProcedure(Guid id, [FromBody] CompleteNursingProcedureRequestDto dto, CancellationToken ct = default)
    {
        Guid nurseId = _currentUserService.UserId ?? Guid.Empty;
        if (nurseId == Guid.Empty)
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(claim, out var parsedClaim))
            {
                return Unauthorized(new ProblemDetails { Title = "Unauthorized", Detail = "Authenticated clinician identity is required.", Status = 401 });
            }
            nurseId = parsedClaim;
        }

        var result = await _sender.Send(new CompleteNursingProcedureCommand(
            id,
            nurseId,
            dto.PatientTolerance,
            dto.SuppliesUsed,
            dto.Notes), ct);

        return result.Match<IActionResult>(
            onSuccess: completed => Ok(completed),
            onFailure: (error, _) => NotFound(new ProblemDetails { Title = "Not Found", Detail = error, Status = 404 })
        );
    }
}
