using ClinicManagementSystem.Domain.Common;

namespace ClinicManagementSystem.Domain.Events;

public record AppointmentBookedEvent(Guid AppointmentId, Guid DoctorId, Guid PatientId, DateTime ScheduledDateTime) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
