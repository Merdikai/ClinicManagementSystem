using ClinicManagementSystem.Domain.Enums;

namespace ClinicManagementSystem.Domain.Entities;

public class Appointment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PatientId { get; set; }
    public Patient Patient { get; set; } = null!;
    public Guid DoctorId { get; set; }
    public User Doctor { get; set; } = null!;
    public DateTime ScheduledDateTime { get; set; }
    public int DurationMinutes { get; set; } = 30;
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Scheduled;
    public string ReasonForVisit { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public VitalSign? VitalSign { get; set; }
    public Consultation? Consultation { get; set; }
    public MedicalRecord? MedicalRecord { get; set; }
}