using ClinicManagementSystem.Domain.Entities;
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
    {
        return await _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .Include(a => a.VitalSign)
            .Include(a => a.Consultation)
                .ThenInclude(c => c!.Prescription)
                    .ThenInclude(p => p!.PrescriptionItems)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<IEnumerable<Appointment>> GetByPatientIdAsync(Guid patientId)
    {
        return await _context.Appointments
            .Include(a => a.Doctor)
            .Include(a => a.VitalSign)
            .Include(a => a.Consultation)
                .ThenInclude(c => c!.Prescription)
                    .ThenInclude(p => p!.PrescriptionItems)
            .Where(a => a.PatientId == patientId)
            .OrderByDescending(a => a.ScheduledDateTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<Appointment>> GetByDoctorIdAsync(Guid doctorId, DateTime? date)
    {
        var query = _context.Appointments
            .Include(a => a.Patient)
            .Where(a => a.DoctorId == doctorId);

        if (date.HasValue)
        {
            var targetDate = date.Value.Date;
            query = query.Where(a => a.ScheduledDateTime.Date == targetDate);
        }

        return await query.OrderBy(a => a.ScheduledDateTime).ToListAsync();
    }

    public async Task<(IEnumerable<Appointment> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, DateTime? startDate, DateTime? endDate)
    {
        var query = _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .Include(a => a.VitalSign)
            .AsQueryable();

        if (startDate.HasValue)
            query = query.Where(a => a.ScheduledDateTime >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(a => a.ScheduledDateTime <= endDate.Value);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(a => a.ScheduledDateTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<bool> IsSlotAvailableAsync(Guid doctorId, DateTime dateTime, int durationMinutes)
    {
        var requestedEnd = dateTime.AddMinutes(durationMinutes);

        var hasConflict = await _context.Appointments
            .AnyAsync(a => a.DoctorId == doctorId &&
                           a.Status != Domain.Enums.AppointmentStatus.Cancelled &&
                           ((dateTime >= a.ScheduledDateTime && dateTime < a.ScheduledDateTime.AddMinutes(a.DurationMinutes)) ||
                            (requestedEnd > a.ScheduledDateTime && requestedEnd <= a.ScheduledDateTime.AddMinutes(a.DurationMinutes)) ||
                            (dateTime <= a.ScheduledDateTime && requestedEnd >= a.ScheduledDateTime.AddMinutes(a.DurationMinutes))));

        return !hasConflict;
    }

    public async Task AddAsync(Appointment appointment)
    {
        await _context.Appointments.AddAsync(appointment);
        await _context.SaveChangesAsync();
    }

    public void Update(Appointment appointment)
    {
        _context.Appointments.Update(appointment);
        _context.SaveChanges();
    }

    public async Task SoftDeleteAsync(Guid id)
    {
        var appt = await _context.Appointments.FindAsync(id);
        if (appt != null)
        {
            appt.IsDeleted = true;
            await _context.SaveChangesAsync();
        }
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
