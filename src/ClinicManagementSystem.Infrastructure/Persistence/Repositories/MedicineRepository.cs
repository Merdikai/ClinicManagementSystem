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

    public async Task<(IEnumerable<Medicine> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, string? search)
    {
        var query = _context.Medicines.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(m =>
                EF.Functions.ILike(m.Name, $"%{search}%") ||
                EF.Functions.ILike(m.Code, $"%{search}%"));
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(m => m.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task AddAsync(Medicine medicine)
        => await _context.Medicines.AddAsync(medicine);

    public void Update(Medicine medicine)
        => _context.Medicines.Update(medicine);
}