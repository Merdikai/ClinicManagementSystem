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

    public async Task<Invoice?> GetByIdAsync(Guid id)
        => await _context.Invoices
            .Include(i => i.LineItems)
            .Include(i => i.Payments)
            .Include(i => i.Patient)
            .FirstOrDefaultAsync(i => i.Id == id);

    public async Task<IEnumerable<Invoice>> GetByPatientIdAsync(Guid patientId)
        => await _context.Invoices
            .Where(i => i.PatientId == patientId)
            .Include(i => i.LineItems)
            .Include(i => i.Payments)
            .OrderByDescending(i => i.IssueDate)
            .ToListAsync();

    public async Task AddAsync(Invoice invoice)
        => await _context.Invoices.AddAsync(invoice);

    public void Update(Invoice invoice)
        => _context.Invoices.Update(invoice);

    public async Task SaveChangesAsync()
        => await _context.SaveChangesAsync();
}