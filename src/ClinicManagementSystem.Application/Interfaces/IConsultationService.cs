using ClinicManagementSystem.Application.DTOs;

namespace ClinicManagementSystem.Application.Interfaces;

public interface IConsultationService
{
    Task<ConsultationResponseDto> CreateAsync(CreateConsultationDto dto, Guid doctorId);
}