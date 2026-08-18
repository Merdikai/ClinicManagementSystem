using System.Text;
using ClinicManagementSystem.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Application.Reports.Queries;

public record ExportInvoicesCsvQuery(DateTime? StartDate, DateTime? EndDate) : IRequest<byte[]>;

public class ExportInvoicesCsvQueryHandler : IRequestHandler<ExportInvoicesCsvQuery, byte[]>
{
    private readonly IClinicDbContext _context;

    public ExportInvoicesCsvQueryHandler(IClinicDbContext context)
    {
        _context = context;
    }

    public async Task<byte[]> Handle(ExportInvoicesCsvQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Invoices.AsQueryable();

        if (request.StartDate.HasValue)
            query = query.Where(i => i.IssueDate >= request.StartDate.Value);

        if (request.EndDate.HasValue)
            query = query.Where(i => i.IssueDate <= request.EndDate.Value);

        var invoices = await query.ToListAsync(cancellationToken);
        var sb = new StringBuilder();
        
        sb.AppendLine("InvoiceNumber,PatientId,IssueDate,DueDate,Status,SubTotal,Tax,Discount,Total");
        
        foreach (var inv in invoices)
        {
            sb.AppendLine($"{inv.InvoiceNumber},{inv.PatientId},{inv.IssueDate:yyyy-MM-dd},{inv.DueDate:yyyy-MM-dd},{inv.Status},{inv.SubTotal},{inv.TaxAmount},{inv.DiscountAmount},{inv.TotalAmount}");
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }
}
