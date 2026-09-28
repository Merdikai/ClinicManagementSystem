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
[Route("api/v{version:apiVersion}/lab-inventory")]
[Tags("Laboratory Inventory & Reagents")]
[Authorize(Roles = "Admin,LabTechnician")]
[EnableRateLimiting(RateLimitingConstants.StaffPolicy)]
public class LabInventoryController : ControllerBase
{
    private readonly ISender _sender;

    public LabInventoryController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("reagents")]
    [EndpointSummary("Get all laboratory reagents and kits")]
    [ProducesResponseType(typeof(List<LabReagentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetReagents(
        [FromQuery] string? category = null,
        [FromQuery] string? status = null,
        [FromQuery] bool lowStockOnly = false)
    {
        var result = await _sender.Send(new GetLabReagentsQuery(category, status, lowStockOnly));
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpPost("reagents")]
    [Authorize(Roles = "LabTechnician")]
    [EndpointSummary("Add a new laboratory reagent")]
    [ProducesResponseType(typeof(LabReagentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateReagent([FromBody] CreateLabReagentRequest request)
    {
        var result = await _sender.Send(new CreateLabReagentCommand(request));
        return result.IsSuccess
            ? StatusCode(StatusCodes.Status201Created, result.Value)
            : BadRequest(new ProblemDetails { Title = "Failed to create reagent", Detail = result.Error });
    }

    [HttpPost("reagents/{id:guid}/adjust")]
    [Authorize(Roles = "LabTechnician")]
    [EndpointSummary("Adjust reagent stock quantity (consumption or manual restock)")]
    [ProducesResponseType(typeof(LabReagentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AdjustStock(Guid id, [FromBody] UpdateReagentStockRequest request)
    {
        var result = await _sender.Send(new UpdateReagentStockCommand(id, request));
        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(new ProblemDetails { Title = "Failed to adjust stock", Detail = result.Error });
    }

    [HttpGet("requisitions")]
    [EndpointSummary("Get laboratory supply requisitions")]
    [ProducesResponseType(typeof(List<LabStockRequisitionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRequisitions(
        [FromQuery] string? status = null,
        [FromQuery] string? urgency = null)
    {
        var result = await _sender.Send(new GetLabRequisitionsQuery(status, urgency));
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpPost("requisitions")]
    [Authorize(Roles = "LabTechnician")]
    [EndpointSummary("Submit a new laboratory supply requisition")]
    [ProducesResponseType(typeof(LabStockRequisitionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateRequisition([FromBody] CreateLabRequisitionRequest request)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        Guid.TryParse(userIdStr, out var currentUserId);
        var userName = User.FindFirstValue(ClaimTypes.Name) ?? "Lab Technician";

        var result = await _sender.Send(new CreateLabRequisitionCommand(request, currentUserId, userName));
        return result.IsSuccess
            ? StatusCode(StatusCodes.Status201Created, result.Value)
            : BadRequest(new ProblemDetails { Title = "Failed to submit requisition", Detail = result.Error });
    }

    [HttpPut("requisitions/{id:guid}/review")]
    [Authorize(Roles = "LabTechnician")]
    [EndpointSummary("Approve, reject, or fulfill a laboratory supply requisition")]
    [ProducesResponseType(typeof(LabStockRequisitionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ReviewRequisition(Guid id, [FromBody] ReviewLabRequisitionRequest request)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        Guid.TryParse(userIdStr, out var currentUserId);
        var userName = User.FindFirstValue(ClaimTypes.Name) ?? "Laboratory Manager";

        var result = await _sender.Send(new ReviewLabRequisitionCommand(id, request, currentUserId, userName));
        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(new ProblemDetails { Title = "Failed to review requisition", Detail = result.Error });
    }
}
