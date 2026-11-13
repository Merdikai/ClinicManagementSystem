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

    public async Task<VitalSign?> GetByAppointmentIdAsync(Guid appointmentId, CancellationToken ct = default)
        => await _context.VitalSigns
            .AsNoTracking()
            .FirstOrDefaultAsync(vs => vs.AppointmentId == appointmentId, ct)
            .ConfigureAwait(false);

    public async Task AddAsync(VitalSign vitalSign, CancellationToken ct = default)
        => await _context.VitalSigns.AddAsync(vitalSign, ct).ConfigureAwait(false);

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _context.SaveChangesAsync(ct).ConfigureAwait(false);
}
