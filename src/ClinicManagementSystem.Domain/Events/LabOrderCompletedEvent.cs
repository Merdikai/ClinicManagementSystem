using ClinicManagementSystem.Domain.Common;

namespace ClinicManagementSystem.Domain.Events;

public record LabOrderCompletedEvent(
    Guid OrderId,
    Guid PatientId,
    string OrderNumber,
    string Status,
    bool HasCriticalAbnormalities
) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
