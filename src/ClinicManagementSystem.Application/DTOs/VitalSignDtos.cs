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

public record VitalSignResponseDto(
    Guid Id,
    decimal SystolicBP,
    decimal DiastolicBP,
    decimal TemperatureC,
    decimal HeartRateBpm,
    decimal RespiratoryRate,
    decimal WeightKg,
    decimal HeightCm,
    DateTime RecordedAt,
    string RecordedByNurse
);