using ClinicManagementSystem.Domain.Entities;

namespace ClinicManagementSystem.Domain.Interfaces;

public interface IVitalSignRepository
{
    Task<VitalSign?> GetByAppointmentIdAsync(Guid appointmentId);
    Task AddAsync(VitalSign vitalSign);
}