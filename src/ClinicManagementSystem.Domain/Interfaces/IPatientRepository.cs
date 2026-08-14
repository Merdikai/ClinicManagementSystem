using ClinicManagementSystem.Domain.Entities;

namespace ClinicManagementSystem.Domain.Interfaces;

public interface IPatientRepository
{
    Task<Patient?> GetByIdAsync(Guid id);
    Task<Patient?> GetByMedicalRecordNumberAsync(string mrn);
    Task<IEnumerable<Patient>> GetAllAsync();
    Task<(IEnumerable<Patient> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, string? search, string? sortBy, bool descending);
    Task AddAsync(Patient patient);
    void Update(Patient patient);
    Task SaveChangesAsync();
}