using ClinicManagementSystem.Domain.Entities;
using ClinicManagementSystem.Domain.Interfaces;
using ClinicManagementSystem.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Infrastructure.Persistence.Repositories;

public class MedicineRepository : IMedicineRepository
{
    private readonly ClinicDbContext _context;

    public MedicineRepository(ClinicDbContext context)
    {
        _context = context;
    }

    public async Task<Medicine?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Medicines.FindAsync([id], ct).ConfigureAwait(false);

    public async Task<IEnumerable<Medicine>> GetAllAsync(CancellationToken ct = default)
        => await _context.Medicines.AsNoTracking().OrderBy(m => m.Name).ToListAsync(ct).ConfigureAwait(false);

    public async Task<(IEnumerable<Medicine> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, string? search, string? sortBy, bool descending, CancellationToken ct = default)
    {
        var query = _context.Medicines.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(m =>
                EF.Functions.ILike(m.Name, $"%{search}%") ||
                EF.Functions.ILike(m.Code, $"%{search}%") ||
                (m.GenericName != null && EF.Functions.ILike(m.GenericName, $"%{search}%")) ||
                EF.Functions.ILike(m.Category, $"%{search}%"));
        }

        var totalCount = await query.CountAsync(ct).ConfigureAwait(false);

        query = sortBy?.ToLower() switch
        {
            "name" => descending ? query.OrderByDescending(m => m.Name) : query.OrderBy(m => m.Name),
            "code" => descending ? query.OrderByDescending(m => m.Code) : query.OrderBy(m => m.Code),
            "unitprice" => descending ? query.OrderByDescending(m => m.UnitPrice) : query.OrderBy(m => m.UnitPrice),
            _ => query.OrderBy(m => m.Name)
        };

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return (items, totalCount);
    }

    public async Task AddAsync(Medicine medicine, CancellationToken ct = default)
        => await _context.Medicines.AddAsync(medicine, ct).ConfigureAwait(false);

    public void Update(Medicine medicine)
        => _context.Medicines.Update(medicine);

    public async Task BulkUpdatePricesAsync(Dictionary<Guid, decimal> priceUpdates, CancellationToken ct = default)
    {
        foreach (var (id, newPrice) in priceUpdates)
        {
            await _context.Medicines
                .Where(m => m.Id == id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(m => m.UnitPrice, newPrice), ct)
                .ConfigureAwait(false);
        }
    }

    public async Task BulkRestockAsync(Dictionary<Guid, int> restockQuantities, CancellationToken ct = default)
    {
        foreach (var (id, quantity) in restockQuantities)
        {
            await _context.Medicines
                .Where(m => m.Id == id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(m => m.StockQuantity, m => m.StockQuantity + quantity), ct)
                .ConfigureAwait(false);
        }
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _context.SaveChangesAsync(ct).ConfigureAwait(false);
}
