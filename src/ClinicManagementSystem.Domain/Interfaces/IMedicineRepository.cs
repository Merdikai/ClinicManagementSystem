using ClinicManagementSystem.Domain.Entities;

namespace ClinicManagementSystem.Domain.Interfaces;

public interface IMedicineRepository
{
    Task<Medicine?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<Medicine>> GetAllAsync(CancellationToken ct = default);
    Task<(IEnumerable<Medicine> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, string? search, string? sortBy, bool descending, CancellationToken ct = default);
    Task AddAsync(Medicine medicine, CancellationToken ct = default);
    void Update(Medicine medicine);
    Task BulkUpdatePricesAsync(Dictionary<Guid, decimal> priceUpdates, CancellationToken ct = default);
    Task BulkRestockAsync(Dictionary<Guid, int> restockQuantities, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
