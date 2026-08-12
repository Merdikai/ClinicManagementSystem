using ClinicManagementSystem.Domain.Entities;

namespace ClinicManagementSystem.Domain.Interfaces;

public interface IMedicineRepository
{
    Task<Medicine?> GetByIdAsync(Guid id);
    Task<IEnumerable<Medicine>> GetAllAsync();
    Task<(IEnumerable<Medicine> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, string? search);
    Task AddAsync(Medicine medicine);
    void Update(Medicine medicine);
    Task SaveChangesAsync();
}