using System.Collections.Concurrent;
using System.Threading.Channels;
using ClinicManagementSystem.Application.Services;

namespace ClinicManagementSystem.Infrastructure.Services;

public class AsyncReportQueue : IAsyncReportQueue
{
    public const int MaxQueuedReports = 50;

    private readonly Channel<AsyncReportJob> _channel;
    private readonly ConcurrentDictionary<string, AsyncReportStatus> _statuses = new();
    private readonly ConcurrentDictionary<string, AsyncReportArtifact> _artifacts = new();

    public AsyncReportQueue()
    {
        // Bounded channel prevents memory exhaustion under spike traffic by applying backpressure
        _channel = Channel.CreateBounded<AsyncReportJob>(new BoundedChannelOptions(MaxQueuedReports)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false,
            SingleWriter = false
        });
    }

    public async ValueTask EnqueueAsync(AsyncReportJob job, CancellationToken cancellationToken = default)
    {
        _statuses[job.JobId] = new AsyncReportStatus(
            job.JobId,
            job.ReportType,
            "Queued",
            job.QueuedAt
        );

        await _channel.Writer.WriteAsync(job, cancellationToken).ConfigureAwait(false);
    }

    public IAsyncEnumerable<AsyncReportJob> ReadAllAsync(CancellationToken cancellationToken = default)
    {
        return _channel.Reader.ReadAllAsync(cancellationToken);
    }

    public void UpdateStatus(string jobId, string status, string? downloadUrl = null, string? error = null)
    {
        _statuses.AddOrUpdate(
            jobId,
            id => new AsyncReportStatus(id, "Unknown", status, DateTime.UtcNow, DateTime.UtcNow, downloadUrl, error),
            (id, old) => old with
            {
                Status = status,
                CompletedAt = status is "Completed" or "Failed" ? DateTime.UtcNow : old.CompletedAt,
                DownloadUrl = downloadUrl ?? old.DownloadUrl,
                ErrorMessage = error ?? old.ErrorMessage
            }
        );
    }

    public AsyncReportStatus? GetStatus(string jobId)
    {
        return _statuses.TryGetValue(jobId, out var status) ? status : null;
    }

    public void SaveArtifact(string jobId, byte[] content, string contentType, string fileName)
    {
        _artifacts[jobId] = new AsyncReportArtifact(content, contentType, fileName);
    }

    public AsyncReportArtifact? GetArtifact(string jobId)
    {
        return _artifacts.TryGetValue(jobId, out var artifact) ? artifact : null;
    }
}
