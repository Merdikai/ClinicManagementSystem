using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Asp.Versioning;
using ClinicManagementSystem.API.Constants;
using ClinicManagementSystem.Application.Pharmacy.Commands;
using ClinicManagementSystem.Application.Pharmacy.DTOs;
using ClinicManagementSystem.Application.Pharmacy.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClinicManagementSystem.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/pharmacy")]
[Authorize(Roles = "Admin,Pharmacist")]
[Tags("Pharmacy & Dispensary Control")]
[EnableRateLimiting(RateLimitingConstants.StaffPolicy)]
public class PharmacyController : ControllerBase
{
    private readonly ISender _sender;

    public PharmacyController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("metrics")]
    [EndpointSummary("Get real-time dispensary KPIs and metrics")]
    [ProducesResponseType(typeof(PharmacyMetricsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMetrics()
    {
        var result = await _sender.Send(new GetPharmacyMetricsQuery());
        return Ok(result);
    }

    [HttpGet("requisitions")]
    [EndpointSummary("Get all pharmacy stock purchase orders and ward transfers")]
    [ProducesResponseType(typeof(List<PharmacyStockRequisitionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRequisitions([FromQuery] string? status = null)
    {
        var result = await _sender.Send(new GetStockRequisitionsQuery(status));
        return Ok(result);
    }

    [HttpPost("requisitions")]
    [Authorize(Roles = "Pharmacist")]
    [EndpointSummary("Create a new stock purchase order or internal ward requisition")]
    [ProducesResponseType(typeof(PharmacyStockRequisitionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateRequisition([FromBody] CreateStockRequisitionDto dto)
    {
        Guid? userId = GetCurrentUserId();
        var command = new CreateStockRequisitionCommand(
            dto.MedicineId,
            dto.QuantityRequested,
            dto.Type,
            dto.Department,
            dto.SupplierName,
            dto.Notes,
            userId
        );

        var result = await _sender.Send(command);
        return result.Match<IActionResult>(
            onSuccess: req => CreatedAtAction(nameof(GetRequisitions), new { version = "1.0" }, req),
            onFailure: (error, code) => BadRequest(new ProblemDetails { Title = "Requisition Creation Failed", Detail = error, Status = 400 })
        );
    }

    [HttpPatch("requisitions/{id:guid}")]
    [Authorize(Roles = "Pharmacist")]
    [EndpointSummary("Approve, reject, or fulfill a pharmacy stock requisition")]
    [ProducesResponseType(typeof(PharmacyStockRequisitionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ReviewRequisition(Guid id, [FromBody] ReviewStockRequisitionDto dto)
    {
        Guid? userId = GetCurrentUserId();
        var command = new ReviewStockRequisitionCommand(id, dto.Status, dto.QuantityApproved, dto.ReviewNotes, userId);
        var result = await _sender.Send(command);

        return result.Match<IActionResult>(
            onSuccess: req => Ok(req),
            onFailure: (error, code) => BadRequest(new ProblemDetails { Title = "Review Failed", Detail = error, Status = 400 })
        );
    }


    [HttpGet("controlled-substances")]
    [EndpointSummary("Get DEA/regulatory audit ledger for controlled substance dispensing")]
    [ProducesResponseType(typeof(List<ControlledSubstanceLogDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetControlledSubstanceLogs()
    {
        var result = await _sender.Send(new GetControlledSubstanceLogsQuery());
        return Ok(result);
    }

    private Guid? GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out var uid) ? uid : null;
    }
}
