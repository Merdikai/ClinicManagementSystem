using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.VitalSigns.Commands;
using ClinicManagementSystem.Application.VitalSigns.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;

namespace ClinicManagementSystem.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/vital-signs")]
[Tags("Vital Signs")]
public class VitalSignsController : ControllerBase
{
    private readonly ISender _sender;

    public VitalSignsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    [EndpointSummary("Record patient vital signs for an appointment")]
    [ProducesResponseType(typeof(VitalSignResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RecordVitals([FromBody] RecordVitalsDto dto, [FromQuery] Guid? nurseId)
    {
        Guid resolvedNurseId = (nurseId.HasValue && nurseId.Value != Guid.Empty) ? nurseId.Value : Guid.Empty;
        if (resolvedNurseId == Guid.Empty)
        {
            var claim = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(claim, out var parsedId))
            {
                resolvedNurseId = parsedId;
            }
        }

        var command = new RecordVitalsCommand(
            dto.AppointmentId,
            dto.SystolicBP,
            dto.DiastolicBP,
            dto.TemperatureC,
            dto.HeartRateBpm,
            dto.RespiratoryRate,
            dto.WeightKg,
            dto.HeightCm,
            resolvedNurseId);

        var result = await _sender.Send(command);
        return result.Match<IActionResult>(
            onSuccess: vitals => CreatedAtAction(nameof(GetByAppointmentId), new { appointmentId = dto.AppointmentId }, vitals),
            onFailure: (error, code) => BadRequest(new ProblemDetails { Title = "Record Vitals Failed", Detail = error, Status = 400 })
        );
    }

    [HttpGet("appointment/{appointmentId:guid}")]
    [EndpointSummary("Get vital signs for an appointment")]
    [ProducesResponseType(typeof(VitalSignResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByAppointmentId(Guid appointmentId)
    {
        var result = await _sender.Send(new GetVitalSignsByAppointmentIdQuery(appointmentId));
        if (result is null)
            return NotFound(new ProblemDetails { Title = "Not Found", Detail = "No vital signs recorded for this appointment", Status = 404 });
        return Ok(result);
    }
}
