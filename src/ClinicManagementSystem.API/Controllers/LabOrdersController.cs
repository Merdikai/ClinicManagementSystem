using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Asp.Versioning;
using ClinicManagementSystem.API.Constants;
using ClinicManagementSystem.Application.Common;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.Laboratories.Commands;
using ClinicManagementSystem.Application.Laboratories.DTOs;
using ClinicManagementSystem.Application.Laboratories.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClinicManagementSystem.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/lab-orders")]
[Tags("Laboratory Orders")]
[Authorize(Roles = "Admin,Doctor,LabTechnician")]
[EnableRateLimiting(RateLimitingConstants.StaffPolicy)]
public class LabOrdersController : ControllerBase
{
    private readonly ISender _sender;

    public LabOrdersController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [EndpointSummary("Get paginated list of lab orders")]
    [ProducesResponseType(typeof(PagedResponse<LabOrderDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] string? status = null,
        [FromQuery] string? priority = null,
        [FromQuery] Guid? patientId = null)
    {
        var result = await _sender.Send(new GetLabOrdersPagedQuery(page, pageSize, search, status, priority, patientId));
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpGet("patient/{patientId:guid}")]
    [EndpointSummary("Get all lab orders for a patient")]
    [ProducesResponseType(typeof(List<LabOrderDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByPatient(Guid patientId)
    {
        var result = await _sender.Send(new GetLabOrdersByPatientQuery(patientId));
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpGet("metrics")]
    [EndpointSummary("Get laboratory metrics summary")]
    [ProducesResponseType(typeof(LabMetricsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMetrics()
    {
        var result = await _sender.Send(new GetLabMetricsQuery());
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpPost]
    [EndpointSummary("Create a new laboratory order")]
    [ProducesResponseType(typeof(LabOrderDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateLabOrderRequest request)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        Guid.TryParse(userIdStr, out var currentUserId);

        var result = await _sender.Send(new CreateLabOrderCommand(request, currentUserId));
        return result.IsSuccess
            ? StatusCode(StatusCodes.Status201Created, result.Value)
            : BadRequest(new ProblemDetails { Title = "Failed to create lab order", Detail = result.Error });
    }

    [HttpPut("{id:guid}/status")]
    [EndpointSummary("Update laboratory order status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] string newStatus)
    {
        var result = await _sender.Send(new UpdateLabOrderStatusCommand(id, newStatus));
        return result.IsSuccess
            ? Ok(new { success = true, message = $"Order status updated to {newStatus}" })
            : BadRequest(new ProblemDetails { Title = "Failed to update order status", Detail = result.Error });
    }

    [HttpPost("results")]
    [EndpointSummary("Record lab test results for an order item")]
    [ProducesResponseType(typeof(LabResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RecordResult([FromBody] RecordLabResultRequest request)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        Guid.TryParse(userIdStr, out var currentUserId);

        var result = await _sender.Send(new RecordLabResultCommand(request, currentUserId));
        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(new ProblemDetails { Title = "Failed to record lab result", Detail = result.Error });
    }
}

