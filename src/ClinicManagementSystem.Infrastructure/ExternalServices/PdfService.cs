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

    private class ThemeConfig
    {
        public string PrimaryHex { get; set; } = "#0F172A";
        public string AccentHex { get; set; } = "#D97706";
        public string HeaderTextHex { get; set; } = "#FFFFFF";
        public string LightBgHex { get; set; } = "#F8FAFC";
        public string BorderHex { get; set; } = "#E2E8F0";
        public string PillBgHex { get; set; } = "#EEF2F6";
        public string Title { get; set; } = "OFFICIAL MEDICAL INVOICE";
    }

    private static ThemeConfig GetTheme(string? themeName)
    {
        return (themeName?.ToLowerInvariant()) switch
        {
            "modernemerald" or "emerald" => new ThemeConfig
            {
                PrimaryHex = "#065F46",      // Deep Forest Emerald
                AccentHex = "#059669",       // Emerald Green
                HeaderTextHex = "#FFFFFF",
                LightBgHex = "#F0FDF4",      // Soft Mint
                BorderHex = "#A7F3D0",
                PillBgHex = "#ECFDF5",
                Title = "CLINICAL CARE INVOICE & RECEIPT"
            },
            "clinicalcyan" or "cyan" or "blue" => new ThemeConfig
            {
                PrimaryHex = "#0369A1",      // Hospital Deep Sky
                AccentHex = "#0284C7",       // Vibrant Medical Cyan
                HeaderTextHex = "#FFFFFF",
                LightBgHex = "#F0F9FF",      // Soft Cyan White
                BorderHex = "#BAE6FD",
                PillBgHex = "#E0F2FE",
                Title = "MEDICAL CENTER BILLING STATEMENT"
            },
            _ => new ThemeConfig             // ExecutiveNavy (Default)
            {
                PrimaryHex = "#0F172A",      // Dark Navy Slate
                AccentHex = "#B45309",       // Executive Gold Amber
                HeaderTextHex = "#FFFFFF",
                LightBgHex = "#F8FAFC",      // Ultra-clean grey
                BorderHex = "#CBD5E1",
                PillBgHex = "#F1F5F9",
                Title = "EXECUTIVE HEALTHCARE INVOICE"
            }
        };
    }

    public byte[] GenerateInvoicePdf(InvoicePdfData data)
    {
        var theme = GetTheme(data.DesignTheme);

        var primaryColor = Color.FromHex(theme.PrimaryHex);
        var accentColor = Color.FromHex(theme.AccentHex);
        var headerTextColor = Color.FromHex(theme.HeaderTextHex);
        var lightBg = Color.FromHex(theme.LightBgHex);
        var borderColor = Color.FromHex(theme.BorderHex);
        var pillBg = Color.FromHex(theme.PillBgHex);

        // Status Badge Colors
        Color statusBg;
        Color statusBorder;
        Color statusText;
        string statusLabel;

        if (string.Equals(data.Status, "Paid", StringComparison.OrdinalIgnoreCase) || data.BalanceDue <= 0 && data.TotalAmount > 0)
        {
            statusBg = Color.FromHex("#DCFCE7");
            statusBorder = Color.FromHex("#16A34A");
            statusText = Color.FromHex("#15803D");
            statusLabel = "● SETTLED / FULLY PAID";
        }
        else if (string.Equals(data.Status, "PartiallyPaid", StringComparison.OrdinalIgnoreCase) || data.AmountPaid > 0 && data.BalanceDue > 0)
        {
            statusBg = Color.FromHex("#FEF3C7");
            statusBorder = Color.FromHex("#D97706");
            statusText = Color.FromHex("#B45309");
            statusLabel = $"⚠ PARTIALLY PAID (${data.AmountPaid:F2})";
        }
        else
        {
            statusBg = Color.FromHex("#FEE2E2");
            statusBorder = Color.FromHex("#DC2626");
            statusText = Color.FromHex("#B91C1C");
            statusLabel = "● PAYMENT OUTSTANDING";
        }

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(10).FontColor(Color.FromHex("#1E293B")));

                // ==========================
                // HEADER SECTION
                // ==========================
                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        // Left: Clinic Identity
                        row.RelativeItem(3).Column(c =>
                        {
                            c.Item().Row(logoRow =>
                            {
                                logoRow.ConstantItem(32).Height(32).Background(primaryColor).AlignCenter().AlignMiddle()
                                    .Text("+").FontSize(22).Bold().FontColor(Colors.White);

                                logoRow.RelativeItem().PaddingLeft(10).Column(titleCol =>
                                {
                                    titleCol.Item().Text("MEDFLOW HEALTH SYSTEMS").FontSize(16).Bold().FontColor(primaryColor);
                                    titleCol.Item().Text("Excellence in Clinical Care & Diagnostic Medicine").FontSize(8.5f).FontColor(Color.FromHex("#64748B"));
                                });
                            });

                            c.Item().PaddingTop(6).Text("742 Evergreen Healthcare Blvd, Suite 400 • New York, NY 10001").FontSize(8).FontColor(Color.FromHex("#64748B"));
                            c.Item().Text("Billing Desk: +1 (555) 019-2834 • Email: finance@medflowclinic.com").FontSize(8).FontColor(Color.FromHex("#64748B"));
                            c.Item().Text("Accredited Clinical Facility • Tax EIN: 84-2910394").FontSize(7.5f).FontColor(Color.FromHex("#94A3B8"));
                        });

                        // Right: Invoice & Status Banner
                        row.RelativeItem(2).AlignRight().Column(c =>
                        {
                            c.Item().Text(theme.Title).FontSize(11).Bold().FontColor(primaryColor);
                            c.Item().Text(data.InvoiceNumber).FontSize(16).Bold().FontColor(accentColor);

                            c.Item().PaddingTop(4).Container()
                                .Background(statusBg)
                                .Border(1)
                                .BorderColor(statusBorder)
                                .PaddingVertical(3)
                                .PaddingHorizontal(8)
                                .Text(statusLabel).FontSize(8).Bold().FontColor(statusText);
                        });
                    });

                    col.Item().PaddingTop(10).LineHorizontal(1.5f).LineColor(primaryColor);
                });

                // ==========================
                // BODY CONTENT SECTION
                // ==========================
                page.Content().PaddingTop(12).Column(col =>
                {
                    // 1. Patient Info and Invoice Details (Two Column Card)
                    col.Item().Row(row =>
                    {
                        // Patient Card
                        row.RelativeItem().Border(1).BorderColor(borderColor).Background(lightBg).Padding(10).Column(pCol =>
                        {
                            pCol.Item().Text("PATIENT & BILLING RECIPIENT").FontSize(8).Bold().FontColor(Color.FromHex("#64748B"));
                            pCol.Item().PaddingTop(2).Text(data.PatientName).FontSize(12).Bold().FontColor(primaryColor);

                            if (!string.IsNullOrWhiteSpace(data.PatientMrn))
                                pCol.Item().Text($"Medical Record No (MRN): {data.PatientMrn}").FontSize(8.5f).FontColor(Color.FromHex("#334155"));

                            if (!string.IsNullOrWhiteSpace(data.PatientPhone))
                                pCol.Item().Text($"Phone: {data.PatientPhone}").FontSize(8.5f).FontColor(Color.FromHex("#334155"));

                            if (!string.IsNullOrWhiteSpace(data.PatientEmail))
                                pCol.Item().Text($"Email: {data.PatientEmail}").FontSize(8.5f).FontColor(Color.FromHex("#334155"));

                            if (!string.IsNullOrWhiteSpace(data.PatientAddress))
                                pCol.Item().Text($"Address: {data.PatientAddress}").FontSize(8.5f).FontColor(Color.FromHex("#64748B"));
                        });

                        row.ConstantItem(15); // Gap

                        // Invoice Metadata Card
                        row.RelativeItem().Border(1).BorderColor(borderColor).Background(pillBg).Padding(10).Column(mCol =>
                        {
                            mCol.Item().Text("INVOICE PARTICULARS").FontSize(8).Bold().FontColor(Color.FromHex("#64748B"));

                            mCol.Item().PaddingTop(2).Row(r =>
                            {
                                r.RelativeItem().Text("Issue Date:").FontSize(8.5f).FontColor(Color.FromHex("#64748B"));
                                r.RelativeItem().AlignRight().Text($"{data.IssueDate:MMM dd, yyyy}").FontSize(8.5f).Bold();
                            });

                            mCol.Item().PaddingTop(2).Row(r =>
                            {
                                r.RelativeItem().Text("Due Date:").FontSize(8.5f).FontColor(Color.FromHex("#64748B"));
                                r.RelativeItem().AlignRight().Text($"{data.DueDate:MMM dd, yyyy}").FontSize(8.5f).Bold();
                            });

                            mCol.Item().PaddingTop(2).Row(r =>
                            {
                                r.RelativeItem().Text("Department:").FontSize(8.5f).FontColor(Color.FromHex("#64748B"));
                                r.RelativeItem().AlignRight().Text(data.Department).FontSize(8.5f).Bold().FontColor(primaryColor);
                            });

                            if (!string.IsNullOrWhiteSpace(data.PaymentMethod) && data.PaymentMethod != "None")
                            {
                                mCol.Item().PaddingTop(2).Row(r =>
                                {
                                    r.RelativeItem().Text("Payment Method:").FontSize(8.5f).FontColor(Color.FromHex("#64748B"));
                                    r.RelativeItem().AlignRight().Text(data.PaymentMethod).FontSize(8.5f).Bold();
                                });
                            }

                            if (!string.IsNullOrWhiteSpace(data.TransactionReference))
                            {
                                mCol.Item().PaddingTop(2).Row(r =>
                                {
                                    r.RelativeItem().Text("Txn Ref:").FontSize(8).FontColor(Color.FromHex("#64748B"));
                                    r.RelativeItem().AlignRight().Text(data.TransactionReference).FontSize(8).FontColor(Color.FromHex("#334155"));
                                });
                            }
                        });
                    });

                    // 2. Line Items Table
                    col.Item().PaddingTop(16).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(24);   // #
                            columns.RelativeColumn(5);    // Description
                            columns.RelativeColumn(1.2f); // Qty
                            columns.RelativeColumn(2);    // Unit Price
                            columns.RelativeColumn(2);    // Line Total
                        });

                        // Table Header
                        table.Header(header =>
                        {
                            header.Cell().Background(primaryColor).Padding(6).AlignCenter().Text("#").FontSize(8.5f).Bold().FontColor(headerTextColor);
                            header.Cell().Background(primaryColor).Padding(6).Text("Clinical Service / Drug / Procedure").FontSize(8.5f).Bold().FontColor(headerTextColor);
                            header.Cell().Background(primaryColor).Padding(6).AlignCenter().Text("Qty").FontSize(8.5f).Bold().FontColor(headerTextColor);
                            header.Cell().Background(primaryColor).Padding(6).AlignRight().Text("Unit Rate").FontSize(8.5f).Bold().FontColor(headerTextColor);
                            header.Cell().Background(primaryColor).Padding(6).AlignRight().Text("Amount ($)").FontSize(8.5f).Bold().FontColor(headerTextColor);
                        });

                        // Table Rows
                        int index = 1;
                        foreach (var item in data.Items)
                        {
                            var rowBg = (index % 2 == 1) ? Colors.White : lightBg;

                            table.Cell().Background(rowBg).BorderBottom(0.5f).BorderColor(borderColor).Padding(6).AlignCenter().Text(index.ToString()).FontSize(8.5f).FontColor(Color.FromHex("#64748B"));
                            table.Cell().Background(rowBg).BorderBottom(0.5f).BorderColor(borderColor).Padding(6).Text(item.Description).FontSize(9).Bold().FontColor(Color.FromHex("#1E293B"));
                            table.Cell().Background(rowBg).BorderBottom(0.5f).BorderColor(borderColor).Padding(6).AlignCenter().Text(item.Quantity.ToString()).FontSize(8.5f);
                            table.Cell().Background(rowBg).BorderBottom(0.5f).BorderColor(borderColor).Padding(6).AlignRight().Text($"${item.UnitPrice:F2}").FontSize(8.5f);
                            table.Cell().Background(rowBg).BorderBottom(0.5f).BorderColor(borderColor).Padding(6).AlignRight().Text($"${item.LineTotal:F2}").FontSize(9).Bold().FontColor(primaryColor);

                            index++;
                        }

                        if (!data.Items.Any())
                        {
                            table.Cell().ColumnSpan(5).Background(Colors.White).Padding(16).AlignCenter().Text("No itemized entries recorded for this invoice.").FontSize(9).Italic().FontColor(Color.FromHex("#64748B"));
                        }
                    });

                    // 3. Financial Totals & Accounting Sign-Off
                    col.Item().PaddingTop(12).Row(totalsRow =>
                    {
                        // Left: Notes, Payment Verification & Barcode
                        totalsRow.RelativeItem(3).Column(c =>
                        {
                            c.Item().Border(1).BorderColor(borderColor).Background(pillBg).Padding(8).Column(cert =>
                            {
                                cert.Item().Text("OFFICIAL ACCOUNTING CERTIFICATION").FontSize(7.5f).Bold().FontColor(primaryColor);
                                cert.Item().PaddingTop(2).Text("This invoice is an audited record of medical services rendered. Payments made are registered in the clinic's permanent general ledger.").FontSize(7.5f).FontColor(Color.FromHex("#475569"));
                                cert.Item().PaddingTop(4).Text($"Design Theme: {theme.Title}").FontSize(7).Italic().FontColor(Color.FromHex("#94A3B8"));
                            });

                            c.Item().PaddingTop(8).Row(stampRow =>
                            {
                                stampRow.ConstantItem(60).Height(24).Border(1).BorderColor(borderColor).AlignCenter().AlignMiddle()
                                    .Text("||||| ||| ||||").FontSize(10).Bold().FontColor(Color.FromHex("#475569"));

                                stampRow.RelativeItem().PaddingLeft(8).AlignMiddle()
                                    .Text($"SECURE-AUTH: {data.InvoiceNumber}-VERIFIED").FontSize(7).FontColor(Color.FromHex("#64748B"));
                            });
                        });

                        totalsRow.ConstantItem(20); // Spacer

                        // Right: Financial Breakdown Box
                        totalsRow.RelativeItem(2).Border(1).BorderColor(borderColor).Background(lightBg).Padding(10).Column(fCol =>
                        {
                            fCol.Item().Row(r =>
                            {
                                r.RelativeItem().Text("Subtotal:").FontSize(9).FontColor(Color.FromHex("#64748B"));
                                r.RelativeItem().AlignRight().Text($"${data.SubTotal:F2}").FontSize(9);
                            });

                            fCol.Item().PaddingTop(3).Row(r =>
                            {
                                r.RelativeItem().Text("Applicable Tax:").FontSize(9).FontColor(Color.FromHex("#64748B"));
                                r.RelativeItem().AlignRight().Text($"${data.TaxAmount:F2}").FontSize(9);
                            });

                            if (data.DiscountAmount > 0)
                            {
                                fCol.Item().PaddingTop(3).Row(r =>
                                {
                                    r.RelativeItem().Text("Discounts/Waivers:").FontSize(9).FontColor(Color.FromHex("#16A34A"));
                                    r.RelativeItem().AlignRight().Text($"-${data.DiscountAmount:F2}").FontSize(9).Bold().FontColor(Color.FromHex("#16A34A"));
                                });
                            }

                            fCol.Item().PaddingVertical(4).LineHorizontal(1).LineColor(borderColor);

                            fCol.Item().Row(r =>
                            {
                                r.RelativeItem().Text("Total Invoiced:").FontSize(11).Bold().FontColor(primaryColor);
                                r.RelativeItem().AlignRight().Text($"${data.TotalAmount:F2}").FontSize(12).Bold().FontColor(primaryColor);
                            });

                            if (data.AmountPaid > 0)
                            {
                                fCol.Item().PaddingTop(3).Row(r =>
                                {
                                    r.RelativeItem().Text("Amount Settled:").FontSize(9).FontColor(Color.FromHex("#15803D"));
                                    r.RelativeItem().AlignRight().Text($"${data.AmountPaid:F2}").FontSize(9).Bold().FontColor(Color.FromHex("#15803D"));
                                });
                            }

                            fCol.Item().PaddingVertical(3).LineHorizontal(1).LineColor(primaryColor);

                            fCol.Item().Row(r =>
                            {
                                r.RelativeItem().Text("Balance Due:").FontSize(11).Bold().FontColor(data.BalanceDue > 0 ? Color.FromHex("#DC2626") : Color.FromHex("#15803D"));
                                r.RelativeItem().AlignRight().Text($"${data.BalanceDue:F2}").FontSize(13).Bold().FontColor(data.BalanceDue > 0 ? Color.FromHex("#DC2626") : Color.FromHex("#15803D"));
                            });
                        });
                    });
                });

                // ==========================
                // FOOTER SECTION
                // ==========================
                page.Footer().Column(col =>
                {
                    col.Item().LineHorizontal(0.5f).LineColor(borderColor);
                    col.Item().PaddingTop(4).Row(row =>
                    {
                        row.RelativeItem().Text("MedFlow Clinic Management System • Confidential Healthcare Billing Statement").FontSize(7.5f).FontColor(Color.FromHex("#94A3B8"));
                        row.RelativeItem().AlignRight().Text(x =>
                        {
                            x.Span("Page ").FontSize(7.5f).FontColor(Color.FromHex("#94A3B8"));
                            x.CurrentPageNumber().FontSize(7.5f).FontColor(Color.FromHex("#94A3B8"));
                            x.Span(" of ").FontSize(7.5f).FontColor(Color.FromHex("#94A3B8"));
                            x.TotalPages().FontSize(7.5f).FontColor(Color.FromHex("#94A3B8"));
                        });
                    });
                });
            });
        });

        return document.GeneratePdf();
    }
}

