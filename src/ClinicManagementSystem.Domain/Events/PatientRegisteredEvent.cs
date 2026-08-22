using ClinicManagementSystem.Domain.Common;

namespace ClinicManagementSystem.Domain.Events;

public record PatientRegisteredEvent(Guid PatientId, string MedicalRecordNumber, string FullName) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
