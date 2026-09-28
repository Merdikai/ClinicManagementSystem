namespace ClinicManagementSystem.Application.Interfaces;

public class InvoicePdfData
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime IssueDate { get; set; }
    public DateTime DueDate { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string PatientMrn { get; set; } = string.Empty;
    public string PatientPhone { get; set; } = string.Empty;
    public string PatientEmail { get; set; } = string.Empty;
    public string PatientAddress { get; set; } = string.Empty;
    public string Department { get; set; } = "General";
    public string Status { get; set; } = "Unpaid";
    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal BalanceDue { get; set; }
    public string PaymentMethod { get; set; } = "Cash";
    public string? TransactionReference { get; set; }
    public string DesignTheme { get; set; } = "ExecutiveNavy"; // ExecutiveNavy, ModernEmerald, ClinicalCyan
    public List<InvoiceItemPdfData> Items { get; set; } = new();
}

public class InvoiceItemPdfData
{
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}

public interface IPdfService
{
    byte[] GenerateInvoicePdf(InvoicePdfData data);
}
