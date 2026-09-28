using ClinicManagementSystem.Domain.Entities;

namespace ClinicManagementSystem.Domain.Specifications;

public class PendingRadiologyOrdersSpecification : BaseSpecification<RadiologyOrder>
{
    public PendingRadiologyOrdersSpecification(string? modality = null)
        : base(r => (modality == null || r.Modality == modality) &&
                    (r.Status == "Ordered" || r.Status == "InProgress" || r.Status == "Scheduled"))
    {
        AddInclude(r => r.Patient);
        AddInclude(r => r.Doctor);
        ApplyOrderByDescending(r => r.Priority == "Stat" || r.Priority == "Urgent");
    }
}
