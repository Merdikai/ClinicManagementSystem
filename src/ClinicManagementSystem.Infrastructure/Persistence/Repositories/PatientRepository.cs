using ClinicManagementSystem.Domain.Entities;
using ClinicManagementSystem.Domain.Interfaces;
using ClinicManagementSystem.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Infrastructure.Persistence.Repositories;

public class PatientRepository : IPatientRepository
{
    private readonly ClinicDbContext _context;

    public PatientRepository(ClinicDbContext context)
    {
        _context = context;
    }

    public async Task<Patient?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Patients.FindAsync([id], ct).ConfigureAwait(false);

    public async Task<Patient?> GetByMedicalRecordNumberAsync(string mrn, CancellationToken ct = default)
        => await _context.Patients.AsNoTracking().FirstOrDefaultAsync(p => p.MedicalRecordNumber == mrn, ct).ConfigureAwait(false);

    public async Task<IEnumerable<Patient>> GetAllAsync(CancellationToken ct = default)
        => await _context.Patients.AsNoTracking().OrderBy(p => p.LastName).ToListAsync(ct).ConfigureAwait(false);

    public async Task<(IEnumerable<Patient> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, string? search, string? sortBy, bool descending, CancellationToken ct = default)
    {
        var query = _context.Patients.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(p =>
                EF.Functions.ILike(p.FirstName, $"%{search}%") ||
                EF.Functions.ILike(p.LastName, $"%{search}%") ||
                EF.Functions.ILike(p.MedicalRecordNumber, $"%{search}%"));
        }

        var totalCount = await query.CountAsync(ct).ConfigureAwait(false);

        query = sortBy?.ToLower() switch
        {
            "firstname" => descending ? query.OrderByDescending(p => p.FirstName) : query.OrderBy(p => p.FirstName),
            "lastname" => descending ? query.OrderByDescending(p => p.LastName) : query.OrderBy(p => p.LastName),
            "medicalrecordnumber" => descending ? query.OrderByDescending(p => p.MedicalRecordNumber) : query.OrderBy(p => p.MedicalRecordNumber),
            "dateofbirth" => descending ? query.OrderByDescending(p => p.DateOfBirth) : query.OrderBy(p => p.DateOfBirth),
            _ => query.OrderBy(p => p.LastName)
        };

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return (items, totalCount);
    }

    public async Task AddAsync(Patient patient, CancellationToken ct = default)
        => await _context.Patients.AddAsync(patient, ct).ConfigureAwait(false);

    public void Update(Patient patient)
        => _context.Patients.Update(patient);

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _context.SaveChangesAsync(ct).ConfigureAwait(false);
}
