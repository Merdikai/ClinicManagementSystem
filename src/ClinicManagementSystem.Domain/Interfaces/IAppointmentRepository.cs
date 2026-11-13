using ClinicManagementSystem.Domain.Entities;
using ClinicManagementSystem.Domain.Enums;

namespace ClinicManagementSystem.Domain.Interfaces;

public interface IAppointmentRepository
{
    Task<Appointment?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<Appointment>> GetByPatientIdAsync(Guid patientId, CancellationToken ct = default);
    Task<IEnumerable<Appointment>> GetByDoctorIdAsync(Guid doctorId, DateTime? date, CancellationToken ct = default);
    Task<(IEnumerable<Appointment> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        DateTime? startDate,
        DateTime? endDate,
        Guid? patientId = null,
        AppointmentStatus? status = null,
        CancellationToken ct = default);
    Task<bool> IsSlotAvailableAsync(Guid doctorId, DateTime dateTime, int durationMinutes, CancellationToken ct = default);
    Task AddAsync(Appointment appointment, CancellationToken ct = default);
    void Update(Appointment appointment);
    Task SoftDeleteAsync(Guid id, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
