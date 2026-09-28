using ClinicManagementSystem.Domain.Common;

namespace ClinicManagementSystem.Domain.Events;

public record DoctorDelegationCreatedEvent(
    Guid DelegationId,
    Guid DelegatorDoctorId,
    Guid DelegateeDoctorId,
    DateTime StartTime,
    DateTime EndTime
) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
