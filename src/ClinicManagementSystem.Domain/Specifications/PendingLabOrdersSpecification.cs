using ClinicManagementSystem.Domain.Entities;
using ClinicManagementSystem.Domain.Enums;

namespace ClinicManagementSystem.Domain.Specifications;

public class PendingLabOrdersSpecification : BaseSpecification<LabOrder>
{
    public PendingLabOrdersSpecification(LabOrderStatus? status = null)
        : base(l => (!status.HasValue && (l.Status == LabOrderStatus.Ordered || l.Status == LabOrderStatus.SampleCollected || l.Status == LabOrderStatus.InProgress))
                    || (status.HasValue && l.Status == status.Value))
    {
        AddInclude(l => l.Patient);
        AddInclude(l => l.Doctor);
        AddInclude(l => l.Items);
        ApplyOrderByDescending(l => l.Priority == LabOrderPriority.Stat || l.Priority == LabOrderPriority.Urgent);
    }
}
