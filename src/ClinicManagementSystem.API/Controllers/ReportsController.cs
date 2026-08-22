using Asp.Versioning;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.Reports.DTOs;
using ClinicManagementSystem.Application.Reports.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagementSystem.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/reports")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly ISender _sender;

    public ReportsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("dashboard")]
    [EndpointSummary("Get real-time operational dashboard summary KPIs")]
    [ProducesResponseType(typeof(DashboardSummaryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDashboardSummary()
    {
        var summary = await _sender.Send(new GetDashboardSummaryQuery());
        return Ok(summary);
    }

    [HttpGet("daily-revenue")]
    [Authorize(Roles = "Admin,Accountant,Receptionist,Doctor")]
    [EndpointSummary("Get daily revenue report for a specific date")]
    [ProducesResponseType(typeof(DailyRevenueReportDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDailyRevenue([FromQuery] DateTime? date)
    {
        var targetDate = date ?? DateTime.UtcNow;
        var report = await _sender.Send(new GetDailyRevenueQuery(targetDate));
        return Ok(report);
    }

    [HttpGet("top-medicines")]
    [EndpointSummary("Get top prescribed medicines")]
    [ProducesResponseType(typeof(IEnumerable<TopMedicineDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTopMedicines([FromQuery] int count = 5)
    {
        var medicines = await _sender.Send(new GetTopMedicinesQuery(count));
        return Ok(medicines);
    }

    [HttpGet("doctor-appointments")]
    [EndpointSummary("Get appointment counts per doctor for a date range")]
    [ProducesResponseType(typeof(IEnumerable<DoctorAppointmentCountDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDoctorAppointments([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        var start = startDate ?? DateTime.UtcNow.AddDays(-30);
        var end = endDate ?? DateTime.UtcNow;
        var counts = await _sender.Send(new GetDoctorAppointmentCountsQuery(start, end));
        return Ok(counts);
    }

    [HttpGet("export/patients")]
    [Authorize(Roles = "Admin,Accountant,Receptionist,Doctor,Nurse")]
    [EndpointSummary("Export all patients to CSV")]
    public async Task<IActionResult> ExportPatients()
    {
        var csv = await _sender.Send(new ExportPatientsCsvQuery());
        return File(csv, "text/csv", $"patients-{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    [HttpGet("export/invoices")]
    [Authorize(Roles = "Admin,Accountant,Receptionist")]
    [EndpointSummary("Export invoices to CSV")]
    public async Task<IActionResult> ExportInvoices([FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
    {
        var csv = await _sender.Send(new ExportInvoicesCsvQuery(startDate, endDate));
        return File(csv, "text/csv", $"invoices-{DateTime.UtcNow:yyyyMMdd}.csv");
    }
}
