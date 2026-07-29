using ClinicManagementSystem.Domain.Entities;

namespace ClinicManagementSystem.Domain.Interfaces;

public interface IPrescriptionRepository
{
    Task<Prescription?> GetByIdAsync(Guid id);
    Task AddAsync(Prescription prescription);
}