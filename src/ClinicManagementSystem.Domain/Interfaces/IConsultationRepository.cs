using ClinicManagementSystem.Domain.Entities;

namespace ClinicManagementSystem.Domain.Interfaces;

public interface IConsultationRepository
{
    Task<Consultation?> GetByIdAsync(Guid id);
    Task<Consultation?> GetByAppointmentIdAsync(Guid appointmentId);
    Task AddAsync(Consultation consultation);
    void Update(Consultation consultation);
}