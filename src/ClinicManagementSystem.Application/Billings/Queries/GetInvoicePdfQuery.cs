using ClinicManagementSystem.Application.Billings.Queries;
using ClinicManagementSystem.Application.Interfaces;
using MediatR;

namespace ClinicManagementSystem.Application.Billings.Queries;

public record GetInvoicePdfQuery(Guid InvoiceId) : IRequest<(byte[]? PdfBytes, string? InvoiceNumber)>;

public class GetInvoicePdfQueryHandler : IRequestHandler<GetInvoicePdfQuery, (byte[]? PdfBytes, string? InvoiceNumber)>
{
    private readonly ISender _sender;
    private readonly IPdfService _pdfService;

    public GetInvoicePdfQueryHandler(ISender sender, IPdfService pdfService)
    {
        _sender = sender;
        _pdfService = pdfService;
    }

    public async Task<(byte[]? PdfBytes, string? InvoiceNumber)> Handle(GetInvoicePdfQuery request, CancellationToken cancellationToken)
    {
        var invoice = await _sender.Send(new GetInvoiceByIdQuery(request.InvoiceId), cancellationToken);
        if (invoice is null)
            return (null, null);

        var pdfData = new InvoicePdfData
        {
            InvoiceNumber = invoice.InvoiceNumber,
            IssueDate = invoice.IssueDate,
            DueDate = invoice.DueDate,
            PatientName = invoice.PatientName,
            SubTotal = invoice.SubTotal,
            TaxAmount = invoice.TaxAmount,
            DiscountAmount = invoice.DiscountAmount,
            TotalAmount = invoice.TotalAmount,
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
