using ClinicManagementSystem.Domain.Entities;
using ClinicManagementSystem.Domain.Interfaces;
using ClinicManagementSystem.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Infrastructure.Persistence.Repositories;

public class VitalSignRepository : IVitalSignRepository
{
    private readonly ClinicDbContext _context;

    public VitalSignRepository(ClinicDbContext context)
    {
        _context = context;
    }

    public async Task<VitalSign?> GetByAppointmentIdAsync(Guid appointmentId)
        => await _context.VitalSigns
            .FirstOrDefaultAsync(vs => vs.AppointmentId == appointmentId);

    public async Task AddAsync(VitalSign vitalSign)
        => await _context.VitalSigns.AddAsync(vitalSign);

    public async Task SaveChangesAsync()
        => await _context.SaveChangesAsync();
}