using ClinicManagementSystem.Domain.Entities;

namespace ClinicManagementSystem.Domain.Interfaces;

public interface IDoctorScheduleRepository
{
    Task<DoctorSchedule?> GetByIdAsync(Guid id);
    Task<IEnumerable<DoctorSchedule>> GetByDoctorIdAsync(Guid doctorId);
    Task AddAsync(DoctorSchedule schedule);
    void Update(DoctorSchedule schedule);
    void Delete(DoctorSchedule schedule);
}
