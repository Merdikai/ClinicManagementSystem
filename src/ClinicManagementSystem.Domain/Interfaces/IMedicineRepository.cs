using ClinicManagementSystem.Domain.Entities;

namespace ClinicManagementSystem.Domain.Interfaces;

public interface IMedicineRepository
{
    Task<Medicine?> GetByIdAsync(Guid id);
    Task<IEnumerable<Medicine>> GetAllAsync();
    Task<(IEnumerable<Medicine> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, string? search, string? sortBy, bool descending);
    Task AddAsync(Medicine medicine);
    void Update(Medicine medicine);
    Task BulkUpdatePricesAsync(Dictionary<Guid, decimal> priceUpdates);
    Task BulkRestockAsync(Dictionary<Guid, int> restockQuantities);
    Task SaveChangesAsync();
}