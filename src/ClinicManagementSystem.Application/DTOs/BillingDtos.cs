namespace ClinicManagementSystem.Application.DTOs;

public record CreateInvoiceDto(
    Guid PatientId,
    Guid? AppointmentId,
    decimal TaxAmount,
    decimal DiscountAmount,
    List<CreateInvoiceItemDto> Items
);

public record CreateInvoiceItemDto(
    string Description,
    int Quantity,
    decimal UnitPrice
);

public class InvoiceResponseDto
{
    public Guid Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public DateTime IssueDate { get; set; }
    public DateTime DueDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal BalanceDue { get; set; }
    public List<InvoiceItemResponseDto> LineItems { get; set; } = new();
}

public class InvoiceItemResponseDto
{
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}

public record ProcessPaymentDto(
    Guid InvoiceId,
    decimal AmountPaid,
    string PaymentMethod,
    string TransactionReference
);

public class PaymentResponseDto
{
    public Guid Id { get; set; }
    public decimal AmountPaid { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public DateTime PaymentDate { get; set; }
}