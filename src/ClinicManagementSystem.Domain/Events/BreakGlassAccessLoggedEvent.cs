using ClinicManagementSystem.Domain.Common;

namespace ClinicManagementSystem.Domain.Events;

public record BreakGlassAccessLoggedEvent(
    Guid LogId,
    Guid PatientId,
    Guid UserId,
    string Reason,
    DateTime AccessedAt
) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
