using ClinicManagementSystem.Domain.Common;

namespace ClinicManagementSystem.Domain.Events;

public record StaffDutyStatusChangedEvent(
    Guid StaffId,
    string Role,
    bool IsOnDuty,
    DateTime Timestamp
) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
