using ClinicManagementSystem.Application.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ClinicManagementSystem.Infrastructure.ExternalServices;

public class PdfService : IPdfService
{
    public PdfService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] GenerateInvoicePdf(InvoicePdfData data)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(12));

                page.Header().Column(col =>
                {
                    col.Item().Text("Clinic Management System")
                        .FontSize(24)
                        .Bold()
                        .FontColor(Colors.Blue.Medium);

                    col.Item().Text($"Invoice: {data.InvoiceNumber}")
                        .FontSize(16)
                        .Bold();

                    col.Item().Text($"Issue Date: {data.IssueDate:dd/MM/yyyy}");
                    col.Item().Text($"Due Date: {data.DueDate:dd/MM/yyyy}");
                });

                page.Content().Column(col =>
                {
                    col.Item().PaddingVertical(10).Column(patientCol =>
                    {
                        patientCol.Item().Text("Billed To:").Bold();
                        patientCol.Item().Text(data.PatientName);
                        if (!string.IsNullOrWhiteSpace(data.PatientAddress))
                            patientCol.Item().Text(data.PatientAddress);
                    });

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(4); // Description
                            columns.RelativeColumn(1); // Quantity
                            columns.RelativeColumn(2); // Unit Price
                            columns.RelativeColumn(2); // Total
                        });

                        table.Header(header =>
                        {
                            header.Cell().Text("Description").Bold();
                            header.Cell().Text("Qty").Bold();
                            header.Cell().Text("Unit Price").Bold();
                            header.Cell().Text("Total").Bold();
                        });

                        foreach (var item in data.Items)
                        {
                            table.Cell().Text(item.Description);
                            table.Cell().Text(item.Quantity.ToString());
                            table.Cell().Text(item.UnitPrice.ToString("C"));
                            table.Cell().Text(item.LineTotal.ToString("C"));
                        }
                    });

                    col.Item().PaddingTop(10).Column(totals =>
                    {
                        totals.Item().AlignRight().Text($"Subtotal: {data.SubTotal:C}");
                        totals.Item().AlignRight().Text($"Tax: {data.TaxAmount:C}");
                        totals.Item().AlignRight().Text($"Discount: {data.DiscountAmount:C}");
                        totals.Item().AlignRight().Text($"Total: {data.TotalAmount:C}")
                            .FontSize(14)
                            .Bold();
                    });
                });

                page.Footer().AlignCenter().Text("Thank you for choosing our clinic.");
            });
        });

        return document.GeneratePdf();
    }
}
