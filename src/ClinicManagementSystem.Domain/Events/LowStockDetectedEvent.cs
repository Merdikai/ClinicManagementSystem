using ClinicManagementSystem.Domain.Common;

namespace ClinicManagementSystem.Domain.Events;

public record LowStockDetectedEvent(Guid MedicineId, string MedicineName, int CurrentStock) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
