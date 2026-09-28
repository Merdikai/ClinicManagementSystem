using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Asp.Versioning;
using ClinicManagementSystem.API.Constants;
using ClinicManagementSystem.Application.Laboratories.Commands;
using ClinicManagementSystem.Application.Laboratories.DTOs;
using ClinicManagementSystem.Application.Laboratories.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClinicManagementSystem.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/lab-equipment")]
[Tags("Laboratory Equipment & Quality Control")]
[Authorize(Roles = "Admin,LabTechnician")]
[EnableRateLimiting(RateLimitingConstants.StaffPolicy)]
public class LabEquipmentController : ControllerBase
{
    private readonly ISender _sender;

    public LabEquipmentController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("logs")]
    [EndpointSummary("Get all laboratory analyzer calibration and QC logs")]
    [ProducesResponseType(typeof(List<LabEquipmentLogDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLogs(
        [FromQuery] string? equipmentName = null,
        [FromQuery] string? logType = null)
    {
        var result = await _sender.Send(new GetLabEquipmentLogsQuery(equipmentName, logType));
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpPost("logs")]
    [EndpointSummary("Record a calibration run, QC test, or maintenance log")]
    [ProducesResponseType(typeof(LabEquipmentLogDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateLog([FromBody] CreateLabEquipmentLogRequest request)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        Guid.TryParse(userIdStr, out var currentUserId);
        var userName = User.FindFirstValue(ClaimTypes.Name) ?? "Laboratory Technician";

        var result = await _sender.Send(new CreateLabEquipmentLogCommand(request, currentUserId, userName));
        return result.IsSuccess
            ? StatusCode(StatusCodes.Status201Created, result.Value)
            : BadRequest(new ProblemDetails { Title = "Failed to record equipment log", Detail = result.Error });
    }
}
