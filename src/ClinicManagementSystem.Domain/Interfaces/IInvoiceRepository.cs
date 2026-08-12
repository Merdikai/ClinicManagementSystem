using ClinicManagementSystem.Domain.Entities;

namespace ClinicManagementSystem.Domain.Interfaces;

public interface IInvoiceRepository
{
    Task<Invoice?> GetByIdAsync(Guid id);
    Task<IEnumerable<Invoice>> GetByPatientIdAsync(Guid patientId);
    Task AddAsync(Invoice invoice);
    void Update(Invoice invoice);
    Task SaveChangesAsync();
}