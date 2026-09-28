using ClinicManagementSystem.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Application.Billings.Queries;

public record GetInvoicePdfQuery(Guid InvoiceId, string? DesignTheme = "ExecutiveNavy") : IRequest<(byte[]? PdfBytes, string? InvoiceNumber)>;

public class GetInvoicePdfQueryHandler : IRequestHandler<GetInvoicePdfQuery, (byte[]? PdfBytes, string? InvoiceNumber)>
{
    private readonly IClinicDbContext _context;
    private readonly IPdfService _pdfService;

    public GetInvoicePdfQueryHandler(IClinicDbContext context, IPdfService pdfService)
    {
        _context = context;
        _pdfService = pdfService;
    }

    public async Task<(byte[]? PdfBytes, string? InvoiceNumber)> Handle(GetInvoicePdfQuery request, CancellationToken cancellationToken)
    {
        var invoice = await _context.Invoices
            .AsNoTracking()
            .Include(i => i.Patient)
            .Include(i => i.Payments)
            .Include(i => i.LineItems)
            .FirstOrDefaultAsync(i => i.Id == request.InvoiceId, cancellationToken);

        if (invoice is null)
            return (null, null);

        var amountPaid = invoice.Payments?.Sum(p => p.AmountPaid) ?? 0m;
        var lastPayment = invoice.Payments?.OrderByDescending(p => p.PaymentDate).FirstOrDefault();

        var pdfData = new InvoicePdfData
        {
            InvoiceNumber = invoice.InvoiceNumber,
            IssueDate = invoice.IssueDate,
            DueDate = invoice.DueDate,
            PatientName = invoice.Patient != null ? $"{invoice.Patient.FirstName} {invoice.Patient.LastName}".Trim() : "Patient",
            PatientMrn = invoice.Patient?.MedicalRecordNumber ?? "",
            PatientPhone = invoice.Patient?.Phone ?? "",
            PatientEmail = invoice.Patient?.Email ?? "",
            PatientAddress = invoice.Patient?.Address ?? "",
            Department = invoice.Department ?? "General",
            Status = invoice.Status.ToString(),
            SubTotal = invoice.SubTotal,
            TaxAmount = invoice.TaxAmount,
            DiscountAmount = invoice.DiscountAmount,
            TotalAmount = invoice.TotalAmount,
            AmountPaid = amountPaid,
            BalanceDue = invoice.BalanceDue,
            PaymentMethod = lastPayment?.PaymentMethod ?? (amountPaid > 0 ? "Electronic" : "Pending"),
            TransactionReference = lastPayment?.TransactionReference,
            DesignTheme = request.DesignTheme ?? "ExecutiveNavy",
            Items = invoice.LineItems.Select(i => new InvoiceItemPdfData
            {
                Description = i.Description,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                LineTotal = i.LineTotal
            }).ToList()
        };

        var pdfBytes = _pdfService.GenerateInvoicePdf(pdfData);
        return (pdfBytes, invoice.InvoiceNumber);
    }
}
