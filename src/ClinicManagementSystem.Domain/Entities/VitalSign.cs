namespace ClinicManagementSystem.Domain.Entities;

public class VitalSign
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AppointmentId { get; set; }
    public Appointment Appointment { get; set; } = null!;
    public Guid RecordedByUserId { get; set; }
    public User RecordedByUser { get; set; } = null!;
    public Guid? ConsultationId { get; set; }
    public Consultation? Consultation { get; set; }

    public decimal SystolicBP { get; set; }
    public decimal DiastolicBP { get; set; }
    public decimal TemperatureC { get; set; }
    public decimal HeartRateBpm { get; set; }
    public decimal RespiratoryRate { get; set; }
    public decimal WeightKg { get; set; }
    public decimal HeightCm { get; set; }
    public decimal? OxygenSaturation { get; set; } // SpO2 (%) e.g. 98.5
    public int? PainScore { get; set; } // 0-10 scale
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
}