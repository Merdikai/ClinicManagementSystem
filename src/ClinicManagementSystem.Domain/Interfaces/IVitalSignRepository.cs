using ClinicManagementSystem.Domain.Entities;

namespace ClinicManagementSystem.Domain.Interfaces;

public interface IVitalSignRepository
{
    Task<VitalSign?> GetByAppointmentIdAsync(Guid appointmentId, CancellationToken ct = default);
    Task AddAsync(VitalSign vitalSign, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
