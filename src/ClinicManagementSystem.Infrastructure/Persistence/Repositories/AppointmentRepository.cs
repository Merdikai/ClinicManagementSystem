using ClinicManagementSystem.Domain.Entities;
using ClinicManagementSystem.Domain.Enums;
using ClinicManagementSystem.Domain.Interfaces;
using ClinicManagementSystem.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Infrastructure.Persistence.Repositories;

public class AppointmentRepository : IAppointmentRepository
{
    private readonly ClinicDbContext _context;

    public AppointmentRepository(ClinicDbContext context)
    {
        _context = context;
    }

    public async Task<Appointment?> GetByIdAsync(Guid id)
        => await _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .FirstOrDefaultAsync(a => a.Id == id);

    public async Task<IEnumerable<Appointment>> GetByPatientIdAsync(Guid patientId)
        => await _context.Appointments
            .Where(a => a.PatientId == patientId)
            .Include(a => a.Doctor)
            .OrderByDescending(a => a.ScheduledDateTime)
            .ToListAsync();

    public async Task<IEnumerable<Appointment>> GetByDoctorIdAsync(Guid doctorId, DateTime? date)
{
    IQueryable<Appointment> query = _context.Appointments
        .Where(a => a.DoctorId == doctorId)
        .Include(a => a.Patient)
        .Include(a => a.Doctor);

    if (date.HasValue)
    {
        query = query.Where(a => a.ScheduledDateTime.Date == date.Value.Date);
    }

    return await query.OrderBy(a => a.ScheduledDateTime).ToListAsync();
}

    public async Task<bool> IsSlotAvailableAsync(Guid doctorId, DateTime dateTime, int durationMinutes)
    {
        var endTime = dateTime.AddMinutes(durationMinutes);
        return !await _context.Appointments.AnyAsync(a =>
            a.DoctorId == doctorId &&
            a.Status != AppointmentStatus.Cancelled &&
            a.ScheduledDateTime < endTime &&
            a.ScheduledDateTime.AddMinutes(a.DurationMinutes) > dateTime);
    }

    public async Task AddAsync(Appointment appointment)
        => await _context.Appointments.AddAsync(appointment);

    public void Update(Appointment appointment)
        => _context.Appointments.Update(appointment);
}