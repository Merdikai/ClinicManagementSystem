namespace ClinicManagementSystem.Domain.Entities;

public class Consultation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AppointmentId { get; set; }
    public Appointment Appointment { get; set; } = null!;
    public Guid DoctorId { get; set; }
    public User Doctor { get; set; } = null!;
    public string ClinicalNotes { get; set; } = string.Empty;
    public string Symptoms { get; set; } = string.Empty;
    public string Diagnosis { get; set; } = string.Empty;
    public DateTime ConsultedAt { get; set; } = DateTime.UtcNow;

    public Prescription? Prescription { get; set; }
}