using ClinicManagementSystem.Domain.Entities;

namespace ClinicManagementSystem.Domain.Interfaces;

public interface IDoctorScheduleRepository
{
    Task<IEnumerable<DoctorSchedule>> GetByDoctorIdAsync(Guid doctorId);
    Task AddAsync(DoctorSchedule schedule);
    void Update(DoctorSchedule schedule);
}