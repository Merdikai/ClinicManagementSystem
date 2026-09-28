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
    {
        return await _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .Include(a => a.VitalSigns)
            .Include(a => a.Consultation)
                .ThenInclude(c => c!.Prescription)
                    .ThenInclude(p => p!.PrescriptionItems)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<IEnumerable<Appointment>> GetByPatientIdAsync(Guid patientId)
    {
        return await _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .Include(a => a.VitalSigns)
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
            .Include(a => a.Doctor)
            .Include(a => a.VitalSigns)
            .Include(a => a.Consultation)
                .ThenInclude(c => c!.Prescription)
                    .ThenInclude(p => p!.PrescriptionItems)
            .Where(a => a.DoctorId == doctorId);

        if (date.HasValue)
        {
            var start = date.Value.Date;
            var end = start.AddDays(1);
            query = query.Where(a => a.ScheduledDateTime >= start && a.ScheduledDateTime < end);
        }

        return await query.OrderBy(a => a.ScheduledDateTime).ToListAsync();
    }

    public async Task<(IEnumerable<Appointment> Items, int TotalCount)> GetPagedAsync(
        int page, 
        int pageSize, 
        DateTime? startDate, 
        DateTime? endDate, 
        Guid? patientId = null, 
        AppointmentStatus? status = null)
    {
        var query = _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .Include(a => a.VitalSigns)
            .Include(a => a.Consultation)
                .ThenInclude(c => c!.Prescription)
                    .ThenInclude(p => p!.PrescriptionItems)
            .AsNoTracking();

        if (startDate.HasValue)
            query = query.Where(a => a.ScheduledDateTime >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(a => a.ScheduledDateTime <= endDate.Value);

        if (patientId.HasValue && patientId.Value != Guid.Empty)
            query = query.Where(a => a.PatientId == patientId.Value);

        if (status.HasValue)
            query = query.Where(a => a.Status == status.Value);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(a => a.ScheduledDateTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<bool> IsSlotAvailableAsync(Guid doctorId, DateTime dateTime, int durationMinutes)
    {
        var end = dateTime.AddMinutes(durationMinutes);
        return !await _context.Appointments.AnyAsync(a =>
            a.DoctorId == doctorId &&
            a.Status != AppointmentStatus.Cancelled &&
            a.Status != AppointmentStatus.Rejected &&
            a.ScheduledDateTime < end &&
            a.ScheduledDateTime.AddMinutes(a.DurationMinutes) > dateTime);
    }

    public async Task AddAsync(Appointment appointment)
    {
        await _context.Appointments.AddAsync(appointment);
    }

    public void Update(Appointment appointment)
    {
        _context.Appointments.Update(appointment);
    }

    public async Task SoftDeleteAsync(Guid id)
    {
        var appointment = await _context.Appointments.FindAsync(id);
        if (appointment != null)
        {
            appointment.IsDeleted = true;
            appointment.DeletedAt = DateTime.UtcNow;
            appointment.Status = AppointmentStatus.Cancelled;
        }
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
