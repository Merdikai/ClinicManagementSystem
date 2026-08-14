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

    public async Task<Medicine?> GetByIdAsync(Guid id)
        => await _context.Medicines.FindAsync(id);

    public async Task<IEnumerable<Medicine>> GetAllAsync()
        => await _context.Medicines.OrderBy(m => m.Name).ToListAsync();

    public async Task<(IEnumerable<Medicine> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, string? search, string? sortBy, bool descending)
    {
        var query = _context.Medicines.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(m =>
                EF.Functions.ILike(m.Name, $"%{search}%") ||
                EF.Functions.ILike(m.Code, $"%{search}%"));
        }

        var totalCount = await query.CountAsync();

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
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task AddAsync(Medicine medicine)
        => await _context.Medicines.AddAsync(medicine);

    public void Update(Medicine medicine)
        => _context.Medicines.Update(medicine);

    public async Task BulkUpdatePricesAsync(Dictionary<Guid, decimal> priceUpdates)
    {
        foreach (var (id, newPrice) in priceUpdates)
        {
            await _context.Medicines
                .Where(m => m.Id == id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(m => m.UnitPrice, newPrice));
        }
    }

    public async Task BulkRestockAsync(Dictionary<Guid, int> restockQuantities)
    {
        foreach (var (id, quantity) in restockQuantities)
        {
            await _context.Medicines
                .Where(m => m.Id == id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(m => m.StockQuantity, m => m.StockQuantity + quantity));
        }
    }

    public async Task SaveChangesAsync()
        => await _context.SaveChangesAsync();
}