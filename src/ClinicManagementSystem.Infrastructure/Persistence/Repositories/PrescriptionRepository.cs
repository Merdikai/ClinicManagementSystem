using ClinicManagementSystem.Domain.Entities;
using ClinicManagementSystem.Domain.Interfaces;
using ClinicManagementSystem.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Infrastructure.Persistence.Repositories;

public class PrescriptionRepository : IPrescriptionRepository
{
    private readonly ClinicDbContext _context;

    public PrescriptionRepository(ClinicDbContext context)
    {
        _context = context;
    }

    public async Task<Prescription?> GetByIdAsync(Guid id)
        => await _context.Prescriptions
            .Include(p => p.PrescriptionItems)
                .ThenInclude(pi => pi.Medicine)
            .FirstOrDefaultAsync(p => p.Id == id);

    public async Task AddAsync(Prescription prescription)
        => await _context.Prescriptions.AddAsync(prescription);
}