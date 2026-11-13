using ClinicManagementSystem.Domain.Entities;
using ClinicManagementSystem.Domain.Interfaces;
using ClinicManagementSystem.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Infrastructure.Persistence.Repositories;

public class DoctorScheduleRepository : IDoctorScheduleRepository
{
    private readonly ClinicDbContext _context;

    public DoctorScheduleRepository(ClinicDbContext context)
    {
        _context = context;
    }

    public async Task<DoctorSchedule?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.DoctorSchedules
            .Include(ds => ds.Doctor)
            .FirstOrDefaultAsync(ds => ds.Id == id, ct)
            .ConfigureAwait(false);
    }

    public async Task<IEnumerable<DoctorSchedule>> GetAllAsync(CancellationToken ct = default)
    {
        return await _context.DoctorSchedules
            .AsNoTracking()
            .Include(ds => ds.Doctor)
            .OrderBy(ds => ds.Doctor != null ? ds.Doctor.LastName : "")
            .ThenBy(ds => ds.DayOfWeek)
            .ThenBy(ds => ds.StartTime)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<IEnumerable<DoctorSchedule>> GetByDoctorIdAsync(Guid doctorId, CancellationToken ct = default)
    {
        return await _context.DoctorSchedules
            .AsNoTracking()
            .Include(ds => ds.Doctor)
            .Where(ds => ds.DoctorId == doctorId)
            .OrderBy(ds => ds.DayOfWeek)
            .ThenBy(ds => ds.StartTime)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task AddAsync(DoctorSchedule schedule, CancellationToken ct = default)
    {
        await _context.DoctorSchedules.AddAsync(schedule, ct).ConfigureAwait(false);
        await _context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public void Update(DoctorSchedule schedule)
    {
        _context.DoctorSchedules.Update(schedule);
        _context.SaveChanges();
    }

    public void Delete(DoctorSchedule schedule)
    {
        _context.DoctorSchedules.Remove(schedule);
        _context.SaveChanges();
    }
}
