namespace ClinicManagementSystem.Domain.Entities;

public class MedicalRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PatientId { get; set; }
    public Patient Patient { get; set; } = null!;
    public Guid? AppointmentId { get; set; }
    public Appointment? Appointment { get; set; }
    public string Notes { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}