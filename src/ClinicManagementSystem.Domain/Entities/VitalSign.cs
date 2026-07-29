namespace ClinicManagementSystem.Domain.Entities;

public class VitalSign
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AppointmentId { get; set; }
    public Appointment Appointment { get; set; } = null!;
    public Guid RecordedByNurseId { get; set; }
    public User Nurse { get; set; } = null!;

    public decimal SystolicBP { get; set; }
    public decimal DiastolicBP { get; set; }
    public decimal TemperatureC { get; set; }
    public decimal HeartRateBpm { get; set; }
    public decimal RespiratoryRate { get; set; }
    public decimal WeightKg { get; set; }
    public decimal HeightCm { get; set; }
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
}