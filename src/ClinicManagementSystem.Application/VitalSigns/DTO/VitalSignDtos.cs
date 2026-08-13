namespace ClinicManagementSystem.Application.DTOs;

public record RecordVitalsDto(
    Guid AppointmentId,
    decimal SystolicBP,
    decimal DiastolicBP,
    decimal TemperatureC,
    decimal HeartRateBpm,
    decimal RespiratoryRate,
    decimal WeightKg,
    decimal HeightCm
);

public class VitalSignResponseDto
{
    public Guid Id { get; set; }
    public decimal SystolicBP { get; set; }
    public decimal DiastolicBP { get; set; }
    public decimal TemperatureC { get; set; }
    public decimal HeartRateBpm { get; set; }
    public decimal RespiratoryRate { get; set; }
    public decimal WeightKg { get; set; }
    public decimal HeightCm { get; set; }
    public DateTime RecordedAt { get; set; }
    public string RecordedByNurse { get; set; } = string.Empty;
}
