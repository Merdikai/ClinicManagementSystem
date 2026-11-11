namespace ClinicManagementSystem.Application.Services;

public record AsyncReportJob(
    string JobId,
    string ReportType,
    string RequestedBy,
    DateTime QueuedAt,
    string ParametersJson
);

public record AsyncReportStatus(
    string JobId,
    string ReportType,
    string Status, // Queued, Processing, Completed, Failed
    DateTime QueuedAt,
    DateTime? CompletedAt = null,
    string? DownloadUrl = null,
    string? ErrorMessage = null
);

public interface IAsyncReportQueue
{
    ValueTask EnqueueAsync(AsyncReportJob job, CancellationToken cancellationToken = default);
    IAsyncEnumerable<AsyncReportJob> ReadAllAsync(CancellationToken cancellationToken = default);
    void UpdateStatus(string jobId, string status, string? downloadUrl = null, string? error = null);
    AsyncReportStatus? GetStatus(string jobId);
}

