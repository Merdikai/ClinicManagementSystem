using ClinicManagementSystem.Domain.Entities;
using ClinicManagementSystem.Domain.Enums;

namespace ClinicManagementSystem.Domain.Interfaces;

public interface IAppointmentRepository
{
    Task<Appointment?> GetByIdAsync(Guid id);
    Task<IEnumerable<Appointment>> GetByPatientIdAsync(Guid patientId);
    Task<IEnumerable<Appointment>> GetByDoctorIdAsync(Guid doctorId, DateTime? date);
    Task<(IEnumerable<Appointment> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, DateTime? startDate, DateTime? endDate);
    Task<bool> IsSlotAvailableAsync(Guid doctorId, DateTime dateTime, int durationMinutes);
    Task AddAsync(Appointment appointment);
    void Update(Appointment appointment);
    Task SoftDeleteAsync(Guid id);
    Task SaveChangesAsync();
}
