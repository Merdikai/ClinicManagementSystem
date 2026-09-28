using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Asp.Versioning;
using ClinicManagementSystem.API.Constants;
using ClinicManagementSystem.Application.Common;
using ClinicManagementSystem.Application.DTOs;
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
[Route("api/v{version:apiVersion}/lab-orders")]
[Tags("Laboratory Orders")]
[Authorize(Roles = "Admin,Doctor,LabTechnician,Nurse")]
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
    [Authorize(Roles = "Doctor,LabTechnician")]
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
    [Authorize(Roles = "LabTechnician,Nurse")]
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

    [HttpPost("{id:guid}/specimen-intake")]
    [Authorize(Roles = "LabTechnician,Nurse")]
    [EndpointSummary("Intake specimen and generate accession barcode")]
    [ProducesResponseType(typeof(LabOrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SpecimenIntake(Guid id, [FromBody] SpecimenIntakeRequest request)
    {
        request.LabOrderId = id;
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        Guid.TryParse(userIdStr, out var currentUserId);
        var techName = User.FindFirstValue(ClaimTypes.Name) ?? "Laboratory Technician";
        var result = await _sender.Send(new IntakeSpecimenCommand(request, currentUserId, techName));
        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(new ProblemDetails { Title = "Failed to intake specimen", Detail = result.Error });
    }

    [HttpPost("{id:guid}/specimen-reject")]
    [Authorize(Roles = "LabTechnician")]
    [EndpointSummary("Reject specimen with clinical reason")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SpecimenReject(Guid id, [FromBody] SpecimenRejectRequest request)
    {
        request.LabOrderId = id;
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        Guid.TryParse(userIdStr, out var currentUserId);
        var techName = User.FindFirstValue(ClaimTypes.Name) ?? "Laboratory Technician";
        var result = await _sender.Send(new RejectSpecimenCommand(request, currentUserId, techName));
        return result.IsSuccess
            ? Ok(new { success = true, message = "Specimen rejected successfully" })
            : BadRequest(new ProblemDetails { Title = "Failed to reject specimen", Detail = result.Error });
    }

    [HttpPost("results")]
    [Authorize(Roles = "LabTechnician")]
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

    [HttpPost("items/{itemId:guid}/verify")]
    [Authorize(Roles = "LabTechnician,Doctor")]
    [EndpointSummary("Verify and sign off on completed lab result")]
    [ProducesResponseType(typeof(LabResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> VerifyResult(Guid itemId)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        Guid.TryParse(userIdStr, out var currentUserId);

        var result = await _sender.Send(new VerifyLabResultCommand(itemId, currentUserId));
        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(new ProblemDetails { Title = "Failed to verify lab result", Detail = result.Error });
    }

    [HttpPost("{id:guid}/doctor-signoff")]
    [Authorize(Roles = "Doctor")]
    [EndpointSummary("Attending Doctor reviews and signs off on pathology results")]
    [ProducesResponseType(typeof(LabOrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DoctorSignOff(Guid id, [FromBody] DoctorSignOffLabOrderRequest? request)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        Guid.TryParse(userIdStr, out var doctorId);

        var isAdmin = User.IsInRole("Admin");
        var result = await _sender.Send(new DoctorSignOffLabOrderCommand(id, doctorId, request?.Notes, isAdmin));
        return Ok(result);
    }

    [HttpPost("{id:guid}/invoice")]
    [Authorize(Roles = "LabTechnician,Admin,Doctor")]
    [EndpointSummary("Generate and attach an official diagnostic laboratory invoice for this order")]
    [ProducesResponseType(typeof(InvoiceResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GenerateInvoice(Guid id)
    {
        var result = await _sender.Send(new GenerateLabInvoiceCommand(id));
        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(new ProblemDetails { Title = "Failed to generate lab invoice", Detail = result.Error });
    }
}

public class DoctorSignOffLabOrderRequest
{
    public string? Notes { get; set; }
}
