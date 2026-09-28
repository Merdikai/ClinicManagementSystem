using ClinicManagementSystem.Domain.Common;

namespace ClinicManagementSystem.Domain.Events;

public record RadiologyOrderCompletedEvent(
    Guid OrderId,
    Guid PatientId,
    string Modality,
    string Findings,
    Guid RadiologistId
) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
