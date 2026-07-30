using ClinicManagementSystem.Application.DTOs;

namespace ClinicManagementSystem.Application.Interfaces;

public interface IVitalSignService
{
    Task<VitalSignResponseDto> RecordAsync(RecordVitalsDto dto, Guid nurseId);
}