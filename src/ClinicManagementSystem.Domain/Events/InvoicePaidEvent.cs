using ClinicManagementSystem.Domain.Common;

namespace ClinicManagementSystem.Domain.Events;

public record InvoicePaidEvent(Guid InvoiceId, Guid PatientId, decimal AmountPaid) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
