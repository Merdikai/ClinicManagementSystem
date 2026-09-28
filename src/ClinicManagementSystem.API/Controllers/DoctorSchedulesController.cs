using Asp.Versioning;
using ClinicManagementSystem.Application.DoctorSchedules.Commands;
using ClinicManagementSystem.Application.DoctorSchedules.Queries;
using ClinicManagementSystem.Application.DTOs;
using MediatR;
using ClinicManagementSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagementSystem.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/doctor-schedules")]
[Authorize]
public class DoctorSchedulesController : ControllerBase
{
    private readonly ISender _sender;
    private readonly ICurrentUserService _currentUserService;

    public DoctorSchedulesController(ISender sender, ICurrentUserService currentUserService)
    {
        _sender = sender;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    [EndpointSummary("Get all doctor schedules and working shifts across the clinic")]
    [ProducesResponseType(typeof(IEnumerable<DoctorScheduleResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllSchedules([FromQuery] Guid? doctorId = null)
    {
        if (doctorId.HasValue)
        {
            var docResult = await _sender.Send(new GetDoctorSchedulesByDoctorIdQuery(doctorId.Value));
            return Ok(docResult.Value);
        }

        var result = await _sender.Send(new GetAllDoctorSchedulesQuery());
        return Ok(result.Value);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Receptionist")]
    [EndpointSummary("Create a new doctor availability schedule")]
    [ProducesResponseType(typeof(DoctorScheduleResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateSchedule([FromBody] CreateDoctorScheduleDto dto)
    {
        var command = new CreateDoctorScheduleCommand(
            dto.DoctorId,
            dto.DayOfWeek,
            dto.StartTime,
            dto.EndTime,
            dto.SlotDurationMinutes
        );

        var result = await _sender.Send(command);

        return result.Match<IActionResult>(
            onSuccess: schedule => CreatedAtAction(nameof(GetDoctorSchedules), new { doctorId = schedule.DoctorId }, schedule),
            onFailure: (error, errorCode) => errorCode switch
            {
                "doctor_not_found" => NotFound(new ProblemDetails { Title = "Not Found", Detail = error, Status = StatusCodes.Status404NotFound }),
                _ => BadRequest(new ProblemDetails { Title = "Validation Error", Detail = error, Status = StatusCodes.Status400BadRequest })
            }
        );
    }

    [HttpGet("my-schedules")]
    [Authorize(Roles = "Doctor,Admin")]
    [EndpointSummary("Get schedules for the currently logged-in doctor")]
    [ProducesResponseType(typeof(IEnumerable<DoctorScheduleResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMySchedules()
    {
        if (_currentUserService.UserId == null)
            return Unauthorized(new ProblemDetails { Title = "Unauthorized", Detail = "User identity not found in token", Status = 401 });

        var result = await _sender.Send(new GetDoctorSchedulesByDoctorIdQuery(_currentUserService.UserId.Value));
        return Ok(result.Value);
    }

    [HttpGet("doctor/{doctorId:guid}")]
    [EndpointSummary("Get all active schedules for a doctor")]
    [ProducesResponseType(typeof(IEnumerable<DoctorScheduleResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDoctorSchedules(Guid doctorId)
    {
        var result = await _sender.Send(new GetDoctorSchedulesByDoctorIdQuery(doctorId));
        return Ok(result.Value);
    }

    [HttpPatch("{id:guid}/toggle-status")]
    [Authorize(Roles = "Admin,Receptionist")]
    [EndpointSummary("Toggle doctor schedule active status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ToggleScheduleStatus(Guid id)
    {
        var result = await _sender.Send(new ToggleDoctorScheduleStatusCommand(id));

        return result.Match<IActionResult>(
            onSuccess: _ => NoContent(),
            onFailure: (error, errorCode) => NotFound(new ProblemDetails
            {
                Title = "Not Found",
                Detail = error,
                Status = StatusCodes.Status404NotFound
            })
        );
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,Receptionist")]
    [EndpointSummary("Delete a doctor schedule")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSchedule(Guid id)
    {
        var result = await _sender.Send(new DeleteDoctorScheduleCommand(id));

        return result.Match<IActionResult>(
            onSuccess: _ => NoContent(),
            onFailure: (error, errorCode) => NotFound(new ProblemDetails
            {
                Title = "Not Found",
                Detail = error,
                Status = StatusCodes.Status404NotFound
            })
        );
    }
}
