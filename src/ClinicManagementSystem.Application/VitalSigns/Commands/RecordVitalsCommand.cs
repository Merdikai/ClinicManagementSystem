using ClinicManagementSystem.Application.Common;
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
    Guid UserId,
    Guid? ConsultationId = null,
    decimal? OxygenSaturation = null,
    int? PainScore = null
) : IRequest<Result<VitalSignResponseDto>>;

