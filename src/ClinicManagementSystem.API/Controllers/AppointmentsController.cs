using AutoMapper;
using ClinicManagementSystem.Application.Appointments.Commands;
using ClinicManagementSystem.Application.Appointments.Queries;
using ClinicManagementSystem.Application.DTOs;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using ClinicManagementSystem.API.Constants;
using Microsoft.AspNetCore.RateLimiting;

namespace ClinicManagementSystem.API.Controllers;

[ApiController]
[Route("api/v1/appointments")]
[EnableRateLimiting(RateLimitingConstants.PatientPolicy)]
public class AppointmentsController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IMapper _mapper;

    public AppointmentsController(ISender sender, IMapper mapper)
    {
        _sender = sender;
        _mapper = mapper;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAppointmentDto dto)
    {
        var command = new CreateAppointmentCommand(dto.PatientId, dto.DoctorId, dto.ScheduledDateTime, dto.DurationMinutes, dto.ReasonForVisit);
        var appointment = await _sender.Send(command);
        return CreatedAtAction(nameof(GetById), new { id = appointment.Id }, appointment);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var appointment = await _sender.Send(new GetAppointmentByIdQuery(id));
        return Ok(appointment);
    }

    [HttpGet("doctor/{doctorId:guid}")]
    public async Task<IActionResult> GetByDoctor(Guid doctorId, [FromQuery] DateTime? date = null)
    {
        var appointments = await _sender.Send(new GetAppointmentsByDoctorQuery(doctorId, date));
        return Ok(appointments);
    }

    [HttpPatch("{id:guid}/checkin")]
    public async Task<IActionResult> CheckIn(Guid id)
    {
        await _sender.Send(new CheckInAppointmentCommand(id));
        return NoContent();
    }

    [HttpPatch("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        await _sender.Send(new CancelAppointmentCommand(id));
        return NoContent();
    }
}