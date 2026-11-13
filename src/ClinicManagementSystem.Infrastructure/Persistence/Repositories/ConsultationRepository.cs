using ClinicManagementSystem.Domain.Entities;
using ClinicManagementSystem.Domain.Interfaces;
using ClinicManagementSystem.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Infrastructure.Persistence.Repositories;

public class ConsultationRepository : IConsultationRepository
{
    private readonly ClinicDbContext _context;

    public ConsultationRepository(ClinicDbContext context)
    {
        _context = context;
    }

    public async Task<Consultation?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Consultations
            .Include(c => c.Prescription)
                .ThenInclude(p => p!.PrescriptionItems)
                .ThenInclude(pi => pi.Medicine)
            .AsSplitQuery()
            .FirstOrDefaultAsync(c => c.Id == id, ct)
            .ConfigureAwait(false);

    public async Task<Consultation?> GetByAppointmentIdAsync(Guid appointmentId, CancellationToken ct = default)
        => await _context.Consultations
            .FirstOrDefaultAsync(c => c.AppointmentId == appointmentId, ct)
            .ConfigureAwait(false);

    public async Task AddAsync(Consultation consultation, CancellationToken ct = default)
        => await _context.Consultations.AddAsync(consultation, ct).ConfigureAwait(false);

    public void Update(Consultation consultation)
        => _context.Consultations.Update(consultation);

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _context.SaveChangesAsync(ct).ConfigureAwait(false);
}
