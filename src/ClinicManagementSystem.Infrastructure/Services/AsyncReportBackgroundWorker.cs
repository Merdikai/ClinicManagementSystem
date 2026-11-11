using ClinicManagementSystem.Application.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ClinicManagementSystem.Infrastructure.Services;

public class AsyncReportBackgroundWorker : BackgroundService
{
    private readonly IAsyncReportQueue _queue;
    private readonly ILogger<AsyncReportBackgroundWorker> _logger;

    public AsyncReportBackgroundWorker(
        IAsyncReportQueue queue,
        ILogger<AsyncReportBackgroundWorker> logger)
    {
        _queue = queue;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AsyncReportBackgroundWorker started listening for background report jobs.");

        await foreach (var job in _queue.ReadAllAsync(stoppingToken))
        {
            try
            {
                _logger.LogInformation("Processing report job {JobId} ({Type}) requested by {User}", 
                    job.JobId, job.ReportType, job.RequestedBy);

                _queue.UpdateStatus(job.JobId, "Processing");

                // Simulate heavy clinical aggregation / PDF compilation
                await Task.Delay(1500, stoppingToken);

                var downloadUrl = $"/api/v1/reports/download/{job.JobId}";
                _queue.UpdateStatus(job.JobId, "Completed", downloadUrl: downloadUrl);

                _logger.LogInformation("Successfully completed background report job {JobId}", job.JobId);
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

