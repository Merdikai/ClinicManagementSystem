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

    public async Task<Consultation?> GetByIdAsync(Guid id)
        => await _context.Consultations
            .Include(c => c.Prescription)
                .ThenInclude(p => p!.PrescriptionItems)
                .ThenInclude(pi => pi.Medicine)
            .FirstOrDefaultAsync(c => c.Id == id);

    public async Task<Consultation?> GetByAppointmentIdAsync(Guid appointmentId)
        => await _context.Consultations
            .FirstOrDefaultAsync(c => c.AppointmentId == appointmentId);

    public async Task AddAsync(Consultation consultation)
        => await _context.Consultations.AddAsync(consultation);

    public void Update(Consultation consultation)
        => _context.Consultations.Update(consultation);
}