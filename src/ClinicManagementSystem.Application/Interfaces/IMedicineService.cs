using ClinicManagementSystem.Application.DTOs;

namespace ClinicManagementSystem.Application.Interfaces;

public interface IMedicineService
{
    Task<MedicineResponseDto> CreateAsync(CreateMedicineDto dto);
    Task<PagedResponse<MedicineResponseDto>> GetPagedAsync(int page, int pageSize, string? search);
    Task DispenseAsync(Guid medicineId, int quantity);
}