using Asp.Versioning;
using ClinicManagementSystem.Application.DoctorSchedules.Commands;
using ClinicManagementSystem.Application.DoctorSchedules.Queries;
using ClinicManagementSystem.Application.DTOs;
using MediatR;
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

    public DoctorSchedulesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    [Authorize(Roles = "Doctor,Admin,Receptionist")]
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

    [HttpGet("doctor/{doctorId:guid}")]
    [EndpointSummary("Get all active schedules for a doctor")]
    [ProducesResponseType(typeof(IEnumerable<DoctorScheduleResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDoctorSchedules(Guid doctorId)
    {
        var result = await _sender.Send(new GetDoctorSchedulesByDoctorIdQuery(doctorId));
        return Ok(result.Value);
    }

    [HttpPatch("{id:guid}/toggle-status")]
    [Authorize(Roles = "Doctor,Admin,Receptionist")]
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
    [Authorize(Roles = "Doctor,Admin,Receptionist")]
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
