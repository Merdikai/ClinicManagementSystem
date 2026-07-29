namespace ClinicManagementSystem.Domain.Entities;

public class PrescriptionItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PrescriptionId { get; set; }
    public Prescription Prescription { get; set; } = null!;
    public Guid MedicineId { get; set; }
    public Medicine Medicine { get; set; } = null!;
    public int Quantity { get; set; }
    public string DosageInstructions { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice => Quantity * UnitPrice;
}