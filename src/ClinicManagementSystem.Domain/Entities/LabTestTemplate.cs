namespace ClinicManagementSystem.Domain.Entities;

public class LabTestTemplate
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string TestCode { get; set; } = string.Empty;
    public string TestName { get; set; } = string.Empty;
    public string Category { get; set; } = "General"; // Hematology, Biochemistry, Urinalysis, Microbiology, Immunology
    public string? Description { get; set; }
    public string SampleType { get; set; } = "Blood"; // Blood, Urine, Stool, Swab, Sputum, CSF
    public int TurnaroundTimeHours { get; set; } = 24;
    public decimal Price { get; set; }
    public bool IsActive { get; set; } = true;
    public string ParametersJson { get; set; } = "[]"; // JSON array of { name, unit, minRef, maxRef, normalText }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<LabOrderItem> OrderItems { get; set; } = new List<LabOrderItem>();
}
