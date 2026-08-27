using ClinicManagementSystem.Domain.Enums;

namespace ClinicManagementSystem.Domain.Entities;

public class LabOrderItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid LabOrderId { get; set; }
    public LabOrder LabOrder { get; set; } = null!;
    public Guid LabTestTemplateId { get; set; }
    public LabTestTemplate LabTestTemplate { get; set; } = null!;

    public LabOrderStatus Status { get; set; } = LabOrderStatus.Ordered;
    public decimal Price { get; set; }

    public LabResult? Result { get; set; }
}
