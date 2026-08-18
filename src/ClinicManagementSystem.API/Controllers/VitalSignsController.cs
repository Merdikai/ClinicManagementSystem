using Asp.Versioning;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.VitalSigns.Commands;
using ClinicManagementSystem.Application.VitalSigns.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ClinicManagementSystem.API.Constants;
using Microsoft.AspNetCore.RateLimiting;

namespace ClinicManagementSystem.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/vitals")]
[Authorize(Roles = "Nurse,Doctor,Admin")]
[EnableRateLimiting(RateLimitingConstants.StaffPolicy)]
public class VitalSignsController : ControllerBase
{
    private readonly ISender _sender;

    public VitalSignsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    [Authorize(Roles = "Nurse,Admin")]
    [EndpointSummary("Record vital signs")]
    [ProducesResponseType(typeof(VitalSignResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Record([FromBody] RecordVitalsDto dto)
    {
        var nurseId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var command = new RecordVitalsCommand(
            dto.AppointmentId,
            dto.SystolicBP,
            dto.DiastolicBP,
            dto.TemperatureC,
            dto.HeartRateBpm,
            dto.RespiratoryRate,
            dto.WeightKg,
            dto.HeightCm,
            nurseId
        );
        var vitals = await _sender.Send(command);
        return Created(string.Empty, vitals);
    }

    [HttpGet("appointment/{appointmentId:guid}")]
    [EndpointSummary("Get vital signs for an appointment")]
    [ProducesResponseType(typeof(VitalSignResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByAppointment(Guid appointmentId)
    {
        var vitals = await _sender.Send(new GetVitalSignsByAppointmentIdQuery(appointmentId));
        if (vitals is null)
            return NotFound(new ProblemDetails { Title = "Not Found", Detail = $"Vital signs for appointment {appointmentId} not found", Status = 404 });

        return Ok(vitals);
    }
}
