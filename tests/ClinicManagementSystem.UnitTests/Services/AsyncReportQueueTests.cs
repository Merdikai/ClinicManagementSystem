using ClinicManagementSystem.Application.Services;
using ClinicManagementSystem.Infrastructure.Services;
using FluentAssertions;
using Xunit;

namespace ClinicManagementSystem.UnitTests.Services;

public class AsyncReportQueueTests
{
    [Fact]
    public async Task EnqueueAsync_AddsJobAndSetsStatusToQueued()
    {
        var queue = new AsyncReportQueue();
        var job = new AsyncReportJob(
            JobId: "job-101",
            ReportType: "ExecutiveSummary",
            RequestedBy: "admin",
            QueuedAt: DateTime.UtcNow,
            ParametersJson: "{}"
        );

        await queue.EnqueueAsync(job);

        var status = queue.GetStatus("job-101");
        status.Should().NotBeNull();
        status!.Status.Should().Be("Queued");
        status.ReportType.Should().Be("ExecutiveSummary");
    }

    [Fact]
    public void UpdateStatus_UpdatesStatusAndSetsCompletedAt()
    {
        var queue = new AsyncReportQueue();
        queue.UpdateStatus("job-202", "Processing");

        var status1 = queue.GetStatus("job-202");
        status1.Should().NotBeNull();
        status1!.Status.Should().Be("Processing");

        queue.UpdateStatus("job-202", "Completed", downloadUrl: "/api/reports/download/job-202");

        var status2 = queue.GetStatus("job-202");
        status2.Should().NotBeNull();
        status2!.Status.Should().Be("Completed");
        status2.DownloadUrl.Should().Be("/api/reports/download/job-202");
        status2.CompletedAt.Should().NotBeNull();
    }
}

