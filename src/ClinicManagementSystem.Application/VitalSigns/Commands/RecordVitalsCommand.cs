using ClinicManagementSystem.Application.DTOs;
using MediatR;

namespace ClinicManagementSystem.Application.VitalSigns.Commands;

public record RecordVitalsCommand(
    Guid AppointmentId,
    decimal SystolicBP,
    decimal DiastolicBP,
    decimal TemperatureC,
    decimal HeartRateBpm,
    decimal RespiratoryRate,
    decimal WeightKg,
    decimal HeightCm,
    Guid NurseId
) : IRequest<VitalSignResponseDto>;
