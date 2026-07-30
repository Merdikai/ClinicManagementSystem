using ClinicManagementSystem.Application.DTOs;

namespace ClinicManagementSystem.Application.Interfaces;

public interface IPatientService
{
    Task<PatientResponseDto> CreateAsync(CreatePatientDto dto);
    Task<PatientResponseDto?> GetByIdAsync(Guid id);
    Task<PagedResponse<PatientResponseDto>> GetPagedAsync(int page, int pageSize, string? search);
}