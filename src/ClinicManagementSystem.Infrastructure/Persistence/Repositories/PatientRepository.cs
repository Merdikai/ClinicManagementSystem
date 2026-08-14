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

    public async Task<Patient?> GetByIdAsync(Guid id)
        => await _context.Patients.FindAsync(id);

    public async Task<Patient?> GetByMedicalRecordNumberAsync(string mrn)
        => await _context.Patients.FirstOrDefaultAsync(p => p.MedicalRecordNumber == mrn);

    public async Task<IEnumerable<Patient>> GetAllAsync()
        => await _context.Patients.OrderBy(p => p.LastName).ToListAsync();

    public async Task<(IEnumerable<Patient> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, string? search, string? sortBy, bool descending)
    {
        var query = _context.Patients.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(p =>
                EF.Functions.ILike(p.FirstName, $"%{search}%") ||
                EF.Functions.ILike(p.LastName, $"%{search}%") ||
                EF.Functions.ILike(p.MedicalRecordNumber, $"%{search}%"));
        }

        var totalCount = await query.CountAsync();

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
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task AddAsync(Patient patient)
        => await _context.Patients.AddAsync(patient);

    public void Update(Patient patient)
        => _context.Patients.Update(patient);

    public async Task SaveChangesAsync()
        => await _context.SaveChangesAsync();
}