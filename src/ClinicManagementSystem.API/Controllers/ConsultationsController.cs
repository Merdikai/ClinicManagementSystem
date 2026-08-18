using Asp.Versioning;
using ClinicManagementSystem.Application.Consultations.Commands;
using ClinicManagementSystem.Application.Consultations.Queries;
using ClinicManagementSystem.Application.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ClinicManagementSystem.API.Constants;
using Microsoft.AspNetCore.RateLimiting;

namespace ClinicManagementSystem.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/consultations")]
[Authorize(Roles = "Doctor,Admin")]
[EnableRateLimiting(RateLimitingConstants.StaffPolicy)]
public class ConsultationsController : ControllerBase
{
    private readonly ISender _sender;

    public ConsultationsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    [Authorize(Roles = "Doctor")]
    [EndpointSummary("Create a new consultation")]
    [ProducesResponseType(typeof(ConsultationResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateConsultationDto dto)
    {
        var doctorId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var command = new CreateConsultationCommand(dto.AppointmentId, dto.Symptoms, dto.Diagnosis, dto.ClinicalNotes, doctorId);
        var consultation = await _sender.Send(command);
        return Created(string.Empty, consultation);
    }

    [HttpGet("appointment/{appointmentId:guid}")]
    [EndpointSummary("Get consultation details by appointment ID")]
    [ProducesResponseType(typeof(ConsultationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByAppointment(Guid appointmentId)
    {
        var consultation = await _sender.Send(new GetConsultationByAppointmentIdQuery(appointmentId));
        if (consultation is null)
            return NotFound(new ProblemDetails { Title = "Not Found", Detail = $"Consultation for appointment {appointmentId} not found", Status = 404 });

        return Ok(consultation);
    }
}
