using System.Security.Claims;
using System.Text.Json;
using Asp.Versioning;
using ClinicManagementSystem.API.Filters;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.Extensions;
using ClinicManagementSystem.Application.Reports.DTOs;
using ClinicManagementSystem.Application.Reports.Queries;
using ClinicManagementSystem.Application.Services;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagementSystem.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/reports")]
[Authorize(Roles = "Admin,Doctor,Nurse,Receptionist,Pharmacist,LabTechnician,Radiologist,Accountant")]
public class ReportsController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IAsyncReportQueue _reportQueue;

    public ReportsController(ISender sender, IAsyncReportQueue reportQueue)
    {
        _sender = sender;
        _reportQueue = reportQueue;
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
    [EndpointSummary("Get top prescribed medicines with optional data shaping")]
    [ProducesResponseType(typeof(IEnumerable<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTopMedicines([FromQuery] int count = 5, [FromQuery] string? fields = null)
    {
        var medicines = await _sender.Send(new GetTopMedicinesQuery(count));
        var shaped = medicines.ShapeData(fields);
        return Ok(shaped);
    }

    [HttpGet("doctor-appointments")]
    [EndpointSummary("Get appointment counts per doctor for a date range with optional data shaping")]
    [ProducesResponseType(typeof(IEnumerable<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDoctorAppointments([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate, [FromQuery] string? fields = null)
    {
        var start = startDate ?? DateTime.UtcNow.AddDays(-30);
        var end = endDate ?? DateTime.UtcNow;
        var counts = await _sender.Send(new GetDoctorAppointmentCountsQuery(start, end));
        var shaped = counts.ShapeData(fields);
        return Ok(shaped);
    }

    // ─── Bounded Channel Background Worker (202 Accepted + Poll + Download + SignalR Push) ───
    [HttpPost("async-summary")]
    [EndpointSummary("Queue background report generation and return 202 Accepted")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> RequestAsyncReport([FromBody] AsyncReportRequestDto? request, CancellationToken ct)
    {
        var jobId = Guid.NewGuid().ToString("N")[..8];
        var username = User.Identity?.Name ?? "Anonymous";
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        var payload = request ?? new AsyncReportRequestDto();

        var job = new AsyncReportJob(
            jobId,
            payload.ReportType ?? "ClinicalExecutiveSummary",
            username,
            DateTime.UtcNow,
            JsonSerializer.Serialize(payload),
            userId
        );

        await _reportQueue.EnqueueAsync(job, ct);

        var statusUrl = Url.Action(nameof(GetAsyncReportStatus), new { version = "1.0", jobId });
        var downloadUrl = Url.Action(nameof(DownloadAsyncReport), new { version = "1.0", jobId });

        return Accepted(statusUrl, new
        {
            JobId = jobId,
            Status = "Queued",
            StatusUrl = statusUrl,
            DownloadUrl = downloadUrl,
            Message = "Report generation has been queued for background processing."
        });
    }

    [HttpGet("async-summary/{jobId}/status", Name = nameof(GetAsyncReportStatus))]
    [EndpointSummary("Poll status of asynchronous background report job")]
    [ProducesResponseType(typeof(AsyncReportStatus), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetAsyncReportStatus(string jobId)
    {
        var status = _reportQueue.GetStatus(jobId);
        if (status is null)
            return NotFound(new ProblemDetails
            {
                Title = "Report Job Not Found",
                Detail = $"No background report found with ID '{jobId}'.",
                Status = StatusCodes.Status404NotFound
            });

        return Ok(status);
    }

    [HttpGet("download/{jobId}", Name = nameof(DownloadAsyncReport))]
    [EndpointSummary("Download completed asynchronous background report artifact")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public IActionResult DownloadAsyncReport(string jobId)
    {
        var artifact = _reportQueue.GetArtifact(jobId);
        if (artifact is null)
        {
            var status = _reportQueue.GetStatus(jobId);
            if (status is not null && status.Status is "Queued" or "Processing")
            {
                return Conflict(new ProblemDetails
                {
                    Title = "Report Still Generating",
                    Detail = $"Background report '{jobId}' is currently {status.Status}. Retry shortly.",
                    Status = StatusCodes.Status409Conflict
                });
            }

            return NotFound(new ProblemDetails
            {
                Title = "Report Artifact Not Found",
                Detail = $"No completed report artifact exists for job '{jobId}'.",
                Status = StatusCodes.Status404NotFound
            });
        }

        return File(artifact.Content, artifact.ContentType, artifact.FileName);
    }

    [HttpGet("export/patients")]
    [Authorize(Roles = "Admin,Accountant,Receptionist,Doctor,Nurse")]
    [AuditAction("Report", "ExportedPatientsCsv")]
    [EndpointSummary("Export patients to Excel CSV with optional timeframe")]
    public async Task<IActionResult> ExportPatients([FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null, [FromQuery] string? timeframe = null)
    {
        var csv = await _sender.Send(new ExportPatientsCsvQuery(startDate, endDate, timeframe));
        var tfSuffix = string.IsNullOrWhiteSpace(timeframe) ? "" : $"-{timeframe.ToLower()}";
        return File(csv, "text/csv; charset=utf-8", $"patients{tfSuffix}-{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    [HttpGet("export/invoices")]
    [Authorize(Roles = "Admin,Accountant,Receptionist")]
    [AuditAction("Report", "ExportedInvoicesCsv")]
    [EndpointSummary("Export invoices to Excel CSV with optional timeframe")]
    public async Task<IActionResult> ExportInvoices([FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null, [FromQuery] string? timeframe = null)
    {
        var csv = await _sender.Send(new ExportInvoicesCsvQuery(startDate, endDate, timeframe));
        var tfSuffix = string.IsNullOrWhiteSpace(timeframe) ? "" : $"-{timeframe.ToLower()}";
        return File(csv, "text/csv; charset=utf-8", $"invoices{tfSuffix}-{DateTime.UtcNow:yyyyMMdd}.csv");
    }
}
