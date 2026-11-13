using ClinicManagementSystem.Domain.Entities;

namespace ClinicManagementSystem.Domain.Interfaces;

public interface IDoctorScheduleRepository
{
    Task<DoctorSchedule?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<DoctorSchedule>> GetAllAsync(CancellationToken ct = default);
    Task<IEnumerable<DoctorSchedule>> GetByDoctorIdAsync(Guid doctorId, CancellationToken ct = default);
    Task AddAsync(DoctorSchedule schedule, CancellationToken ct = default);
    void Update(DoctorSchedule schedule);
    void Delete(DoctorSchedule schedule);
}
