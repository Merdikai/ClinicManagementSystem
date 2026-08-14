using Asp.Versioning;
using ClinicManagementSystem.Application.Reports.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagementSystem.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/reports")]
[Authorize(Roles = "Admin,Accountant")]
[Tags("Reports")]
public class ReportsController : ControllerBase
{
    private readonly ISender _sender;

    public ReportsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("daily-revenue")]
    [EndpointSummary("Get Daily Revenue")]
    public async Task<IActionResult> GetDailyRevenue([FromQuery] DateTime date)
    {
        var report = await _sender.Send(new GetDailyRevenueQuery(date));
        return Ok(report);
    }

    [HttpGet("top-medicines")]
    [EndpointSummary("Get Top Medicines")]
    public async Task<IActionResult> GetTopMedicines([FromQuery] int count = 5)
    {
        var medicines = await _sender.Send(new GetTopMedicinesQuery(count));
        return Ok(medicines);
    }

    [HttpGet("doctor-appointments")]
    [EndpointSummary("Get Doctor Appointment Counts")]
    public async Task<IActionResult> GetDoctorAppointments([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
    {
        var counts = await _sender.Send(new GetDoctorAppointmentCountsQuery(startDate, endDate));
        return Ok(counts);
    }
}
