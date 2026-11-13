using ClinicManagementSystem.Domain.Entities;
using ClinicManagementSystem.Domain.Interfaces;
using ClinicManagementSystem.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Infrastructure.Persistence.Repositories;

public class InvoiceRepository : IInvoiceRepository
{
    private readonly ClinicDbContext _context;

    public InvoiceRepository(ClinicDbContext context)
    {
        _context = context;
    }

    public async Task<Invoice?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Invoices
            .Include(i => i.LineItems)
            .Include(i => i.Payments)
            .Include(i => i.Patient)
            .AsSplitQuery()
            .FirstOrDefaultAsync(i => i.Id == id, ct)
            .ConfigureAwait(false);

    public async Task<IEnumerable<Invoice>> GetByPatientIdAsync(Guid patientId, CancellationToken ct = default)
        => await _context.Invoices
            .AsNoTracking()
            .Where(i => i.PatientId == patientId)
            .Include(i => i.LineItems)
            .Include(i => i.Payments)
            .Include(i => i.Patient)
            .AsSplitQuery()
            .OrderByDescending(i => i.IssueDate)
            .ToListAsync(ct)
            .ConfigureAwait(false);

    public async Task AddAsync(Invoice invoice, CancellationToken ct = default)
        => await _context.Invoices.AddAsync(invoice, ct).ConfigureAwait(false);

    public void Update(Invoice invoice)
        => _context.Invoices.Update(invoice);

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _context.SaveChangesAsync(ct).ConfigureAwait(false);
}
