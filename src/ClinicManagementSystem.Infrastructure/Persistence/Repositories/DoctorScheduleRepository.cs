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

    public async Task<IEnumerable<DoctorSchedule>> GetByDoctorIdAsync(Guid doctorId)
        => await _context.DoctorSchedules
            .Where(ds => ds.DoctorId == doctorId && ds.IsActive)
            .ToListAsync();

    public async Task AddAsync(DoctorSchedule schedule)
        => await _context.DoctorSchedules.AddAsync(schedule);

    public void Update(DoctorSchedule schedule)
        => _context.DoctorSchedules.Update(schedule);
}