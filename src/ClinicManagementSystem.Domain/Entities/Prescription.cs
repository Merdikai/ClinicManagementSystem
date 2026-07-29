namespace ClinicManagementSystem.Domain.Entities;

public class Prescription
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ConsultationId { get; set; }
    public Consultation Consultation { get; set; } = null!;
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
    public string Notes { get; set; } = string.Empty;

    public ICollection<PrescriptionItem> PrescriptionItems { get; set; } = new List<PrescriptionItem>();
}