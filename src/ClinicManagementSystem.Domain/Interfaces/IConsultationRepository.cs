using ClinicManagementSystem.Domain.Entities;

namespace ClinicManagementSystem.Domain.Interfaces;

public interface IConsultationRepository
{
    Task<Consultation?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Consultation?> GetByAppointmentIdAsync(Guid appointmentId, CancellationToken ct = default);
    Task AddAsync(Consultation consultation, CancellationToken ct = default);
    void Update(Consultation consultation);
    Task SaveChangesAsync(CancellationToken ct = default);
}
