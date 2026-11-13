using Asp.Versioning;
using ClinicManagementSystem.Application.Extensions;
using ClinicManagementSystem.Application.Reports.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagementSystem.API.Controllers.V2;

[ApiController]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/reports")]
[Tags("Reports V2")]
[Authorize(Roles = "Admin,Doctor,Nurse,Receptionist,Pharmacist,LabTechnician,Radiologist,Accountant")]
public class ReportsV2Controller : ControllerBase
{
    private readonly ISender _sender;

    public ReportsV2Controller(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("top-medicines")]
    [EndpointSummary("V2: Top prescribed medicines with SQL/DTO data shaping and API metadata")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTopMedicines([FromQuery] int count = 5, [FromQuery] string? fields = null, CancellationToken ct = default)
    {
        var medicines = await _sender.Send(new GetTopMedicinesQuery(count), ct);
        return Ok(new
        {
            data = medicines.ShapeData(fields),
            meta = new
            {
                apiVersion = "2.0",
                count,
                shapedFields = string.IsNullOrWhiteSpace(fields) ? "all" : fields,
                timestampUtc = DateTime.UtcNow
            }
        });
    }

    [HttpGet("doctor-appointments")]
    [EndpointSummary("V2: Doctor appointment utilization with data shaping and API metadata")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDoctorAppointments(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string? fields = null,
        CancellationToken ct = default)
    {
        var start = startDate ?? DateTime.UtcNow.AddDays(-30);
        var end = endDate ?? DateTime.UtcNow;
        var counts = await _sender.Send(new GetDoctorAppointmentCountsQuery(start, end), ct);

        return Ok(new
        {
            data = counts.ShapeData(fields),
            meta = new
            {
                apiVersion = "2.0",
                rangeStartUtc = start,
                rangeEndUtc = end,
                shapedFields = string.IsNullOrWhiteSpace(fields) ? "all" : fields,
                timestampUtc = DateTime.UtcNow
            }
        });
    }
}

