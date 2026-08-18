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

    public async Task<DoctorSchedule?> GetByIdAsync(Guid id)
    {
        return await _context.DoctorSchedules
            .Include(ds => ds.Doctor)
            .FirstOrDefaultAsync(ds => ds.Id == id);
    }

    public async Task<IEnumerable<DoctorSchedule>> GetByDoctorIdAsync(Guid doctorId)
    {
        return await _context.DoctorSchedules
            .Include(ds => ds.Doctor)
            .Where(ds => ds.DoctorId == doctorId)
            .OrderBy(ds => ds.DayOfWeek)
            .ThenBy(ds => ds.StartTime)
            .ToListAsync();
    }

    public async Task AddAsync(DoctorSchedule schedule)
    {
        await _context.DoctorSchedules.AddAsync(schedule);
        await _context.SaveChangesAsync();
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
