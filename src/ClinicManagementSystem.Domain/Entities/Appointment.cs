using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
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
    public string? TriageUrgency { get; set; } // "Emergency", "Urgent", "Routine"
    public string? RoomAssignment { get; set; } // e.g. "Exam Room 1", "Triage Bay A", "Observation Bed 2"
    public string? TriageNotes { get; set; }
    public string? TokenNumber { get; set; } // Queue Token (e.g. "A-01", "Q-104")
    public string? VisitType { get; set; } = "General Consultation"; // "General Consultation", "Follow-up", "Vaccination", etc.
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public uint RowVersion { get; set; }

    public ICollection<VitalSign> VitalSigns { get; set; } = new List<VitalSign>();
    [NotMapped]
    public VitalSign? VitalSign { get => VitalSigns.OrderByDescending(v => v.RecordedAt).FirstOrDefault(); set {} }
    public Consultation? Consultation { get; set; }
    public MedicalRecord? MedicalRecord { get; set; }
}