using System.Collections.Generic;
using System.Threading.Tasks;
using Asp.Versioning;
using ClinicManagementSystem.API.Constants;
using ClinicManagementSystem.Application.StaffDuty.Commands;
using ClinicManagementSystem.Application.StaffDuty.DTOs;
using ClinicManagementSystem.Application.StaffDuty.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClinicManagementSystem.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/staff-duty")]
[Tags("Staff Active Duty & Clock-In")]
[Authorize]
[EnableRateLimiting(RateLimitingConstants.StaffPolicy)]
public class StaffDutyController : ControllerBase
{
    private readonly ISender _sender;

    public StaffDutyController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("my-status")]
    [EndpointSummary("Get active duty shift status for currently logged-in staff member")]
    [ProducesResponseType(typeof(StaffDutyShiftDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyDutyStatus()
    {
        var result = await _sender.Send(new GetMyDutyStatusQuery());
        return Ok(result);
    }

    [HttpGet("active-staff")]
    [EndpointSummary("Get all staff members currently clocked-in on active duty across departments")]
    [ProducesResponseType(typeof(List<StaffDutyShiftDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActiveStaff()
    {
        var result = await _sender.Send(new GetActiveStaffDutyQuery());
        return Ok(result);
    }

    [HttpPost("clock-in")]
    [EndpointSummary("Clock in to begin active clinical duty shift")]
    [ProducesResponseType(typeof(StaffDutyShiftDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ClockIn([FromBody] ClockInDutyDto dto)
    {
        var result = await _sender.Send(new ClockInDutyCommand(dto));
        return Ok(result);
    }

    [HttpPost("clock-out")]
    [EndpointSummary("Clock out to conclude active duty shift with optional handover notes")]
    [ProducesResponseType(typeof(StaffDutyShiftDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ClockOut([FromBody] ClockOutDutyDto dto)
    {
        var result = await _sender.Send(new ClockOutDutyCommand(dto));
        if (result == null)
            return BadRequest(new ProblemDetails { Title = "Clock-out Failed", Detail = "No active duty shift found to clock out from." });

        return Ok(result);
    }
}
