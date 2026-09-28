using System.Text;
using ClinicManagementSystem.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Application.Reports.Queries;

public record ExportInvoicesCsvQuery(DateTime? StartDate = null, DateTime? EndDate = null, string? Timeframe = null) : IRequest<byte[]>;

public class ExportInvoicesCsvQueryHandler : IRequestHandler<ExportInvoicesCsvQuery, byte[]>
{
    private readonly IClinicDbContext _context;

    public ExportInvoicesCsvQueryHandler(IClinicDbContext context)
    {
        _context = context;
    }

    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
        return value;
    }

    public async Task<byte[]> Handle(ExportInvoicesCsvQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Invoices
            .AsNoTracking()
            .Include(i => i.Patient)
            .Include(i => i.Payments)
            .AsQueryable();

        if (request.StartDate.HasValue)
        {
            var startUtc = DateTime.SpecifyKind(request.StartDate.Value, DateTimeKind.Utc);
            query = query.Where(i => i.IssueDate >= startUtc);
        }

        if (request.EndDate.HasValue)
        {
            var endUtc = DateTime.SpecifyKind(request.EndDate.Value, DateTimeKind.Utc);
            query = query.Where(i => i.IssueDate <= endUtc);
        }

        var invoices = await query
            .OrderByDescending(i => i.IssueDate)
            .ToListAsync(cancellationToken);

        var sb = new StringBuilder();
        sb.AppendLine("InvoiceNumber,IssueDate,DueDate,MRN,PatientName,Department,Status,SubTotal,TaxAmount,DiscountAmount,TotalAmount,AmountPaid,BalanceDue,PaymentMethods");

        foreach (var inv in invoices)
        {
            var patientName = inv.Patient != null ? $"{inv.Patient.FirstName} {inv.Patient.LastName}".Trim() : "Unknown Patient";
            var patientMrn = inv.Patient?.MedicalRecordNumber ?? "";
            var amountPaid = inv.Payments?.Sum(p => p.AmountPaid) ?? 0m;
            var paymentMethods = inv.Payments != null && inv.Payments.Any()
                ? string.Join("; ", inv.Payments.Select(p => p.PaymentMethod).Distinct())
                : "None";

            sb.AppendLine($"{EscapeCsv(inv.InvoiceNumber)},{inv.IssueDate:yyyy-MM-dd},{inv.DueDate:yyyy-MM-dd},{EscapeCsv(patientMrn)},{EscapeCsv(patientName)},{EscapeCsv(inv.Department)},{inv.Status},{inv.SubTotal:F2},{inv.TaxAmount:F2},{inv.DiscountAmount:F2},{inv.TotalAmount:F2},{amountPaid:F2},{inv.BalanceDue:F2},{EscapeCsv(paymentMethods)}");
        }

        // Prepend UTF-8 BOM for Microsoft Excel compatibility
        var bom = new byte[] { 0xEF, 0xBB, 0xBF };
        var textBytes = Encoding.UTF8.GetBytes(sb.ToString());
        var result = new byte[bom.Length + textBytes.Length];
        Buffer.BlockCopy(bom, 0, result, 0, bom.Length);
        Buffer.BlockCopy(textBytes, 0, result, bom.Length, textBytes.Length);

        return result;
    }
}
