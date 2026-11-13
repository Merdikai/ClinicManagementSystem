using System.Globalization;
using System.Text;
using System.Text.Json;
using ClinicManagementSystem.Application.Interfaces;
using ClinicManagementSystem.Application.Services;
using ClinicManagementSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ClinicManagementSystem.Infrastructure.Services;

public class AsyncReportBackgroundWorker : BackgroundService
{
    private readonly IAsyncReportQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AsyncReportBackgroundWorker> _logger;

    public AsyncReportBackgroundWorker(
        IAsyncReportQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<AsyncReportBackgroundWorker> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AsyncReportBackgroundWorker started listening for background report jobs (Channel capacity: {Capacity}).", AsyncReportQueue.MaxQueuedReports);

        await foreach (var job in _queue.ReadAllAsync(stoppingToken).ConfigureAwait(false))
        {
            try
            {
                _logger.LogInformation("Processing report job {JobId} ({Type}) requested by {User}",
                    job.JobId, job.ReportType, job.RequestedBy);

                _queue.UpdateStatus(job.JobId, "Processing");

                await using var scope = _scopeFactory.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<IClinicDbContext>();
                var notifier = scope.ServiceProvider.GetRequiredService<INotificationService>();

                var totalPatients = await db.Patients.AsNoTracking().CountAsync(stoppingToken).ConfigureAwait(false);
                var totalAppointments = await db.Appointments.AsNoTracking().CountAsync(stoppingToken).ConfigureAwait(false);
                var todayStart = DateTime.UtcNow.Date;
                var todayAppointments = await db.Appointments.AsNoTracking()
                    .CountAsync(a => a.ScheduledDateTime >= todayStart, stoppingToken).ConfigureAwait(false);
                var totalBilled = await db.Invoices.AsNoTracking()
                    .SumAsync(i => (decimal?)i.TotalAmount, stoppingToken).ConfigureAwait(false) ?? 0m;
                var totalCollected = await db.Payments.AsNoTracking()
                    .SumAsync(p => (decimal?)p.AmountPaid, stoppingToken).ConfigureAwait(false) ?? 0m;
                var lowStockCount = await db.Medicines.AsNoTracking()
                    .CountAsync(m => m.IsActive && m.StockQuantity <= m.ReorderLevel, stoppingToken).ConfigureAwait(false);
                var pendingLabOrders = await db.LabOrders.AsNoTracking()
                    .CountAsync(o => o.Status != LabOrderStatus.Completed && o.Status != LabOrderStatus.Cancelled, stoppingToken).ConfigureAwait(false);
                var pendingRadiologyOrders = await db.RadiologyOrders.AsNoTracking()
                    .CountAsync(o => o.Status != "Completed" && o.Status != "Cancelled", stoppingToken).ConfigureAwait(false);

                DateTime? startDate = null;
                DateTime? endDate = null;
                if (!string.IsNullOrWhiteSpace(job.ParametersJson))
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(job.ParametersJson);
                        if (doc.RootElement.TryGetProperty("StartDate", out var sProp) && sProp.ValueKind == JsonValueKind.String &&
                            DateTime.TryParse(sProp.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out var parsedStart))
                        {
                            startDate = parsedStart;
                        }
                        if (doc.RootElement.TryGetProperty("EndDate", out var eProp) && eProp.ValueKind == JsonValueKind.String &&
                            DateTime.TryParse(eProp.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out var parsedEnd))
                        {
                            endDate = parsedEnd;
                        }
                    }
                    catch
                    {
                        // Non-fatal parameter parsing fallback
                    }
                }

                var sb = new StringBuilder();
                sb.AppendLine("Section,Metric,Value,GeneratedAtUtc");
                var generatedAt = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);
                sb.AppendLine($"Metadata,ReportJobId,{job.JobId},{generatedAt}");
                sb.AppendLine($"Metadata,ReportType,\"{job.ReportType.Replace("\"", "\"\"")}\",{generatedAt}");
                sb.AppendLine($"Metadata,RequestedBy,\"{job.RequestedBy.Replace("\"", "\"\"")}\",{generatedAt}");
                if (startDate.HasValue)
                    sb.AppendLine($"Filter,StartDateUtc,{startDate.Value:O},{generatedAt}");
                if (endDate.HasValue)
                    sb.AppendLine($"Filter,EndDateUtc,{endDate.Value:O},{generatedAt}");
                sb.AppendLine($"Patients,TotalActivePatients,{totalPatients},{generatedAt}");
                sb.AppendLine($"Appointments,TotalAppointments,{totalAppointments},{generatedAt}");
                sb.AppendLine($"Appointments,ScheduledTodayOrLater,{todayAppointments},{generatedAt}");
                sb.AppendLine($"Finance,TotalBilledUsd,{totalBilled.ToString("F2", CultureInfo.InvariantCulture)},{generatedAt}");
                sb.AppendLine($"Finance,TotalCollectedUsd,{totalCollected.ToString("F2", CultureInfo.InvariantCulture)},{generatedAt}");
                sb.AppendLine($"Pharmacy,LowStockMedicinesCount,{lowStockCount},{generatedAt}");
                sb.AppendLine($"Diagnostics,ActiveLabOrders,{pendingLabOrders},{generatedAt}");
                sb.AppendLine($"Diagnostics,ActiveRadiologyOrders,{pendingRadiologyOrders},{generatedAt}");

                var fileName = $"MedFlow_{job.ReportType}_{job.JobId}.csv";
                var bytes = Encoding.UTF8.GetBytes(sb.ToString());
                _queue.SaveArtifact(job.JobId, bytes, "text/csv; charset=utf-8", fileName);

                var downloadUrl = $"/api/v1/reports/download/{job.JobId}";
                _queue.UpdateStatus(job.JobId, "Completed", downloadUrl: downloadUrl);

                await notifier.NotifyReportReadyAsync(job.RequestedByUserId, job.JobId, job.ReportType, downloadUrl)
                    .ConfigureAwait(false);

                _logger.LogInformation("Successfully completed background report job {JobId} ({ByteCount} bytes)", job.JobId, bytes.Length);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process background report job {JobId}", job.JobId);
                _queue.UpdateStatus(job.JobId, "Failed", error: ex.Message);
            }
        }
    }
}
