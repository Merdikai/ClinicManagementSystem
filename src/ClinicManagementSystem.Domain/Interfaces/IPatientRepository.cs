using ClinicManagementSystem.Domain.Entities;

namespace ClinicManagementSystem.Domain.Interfaces;

public interface IPatientRepository
{
    Task<Patient?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Patient?> GetByMedicalRecordNumberAsync(string mrn, CancellationToken ct = default);
    Task<IEnumerable<Patient>> GetAllAsync(CancellationToken ct = default);
    Task<(IEnumerable<Patient> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, string? search, string? sortBy, bool descending, CancellationToken ct = default);
    Task AddAsync(Patient patient, CancellationToken ct = default);
    void Update(Patient patient);
    Task SaveChangesAsync(CancellationToken ct = default);
}
