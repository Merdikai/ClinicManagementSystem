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

public record InvoiceResponseDto(
    Guid Id,
    string InvoiceNumber,
    string PatientName,
    DateTime IssueDate,
    DateTime DueDate,
    string Status,
    decimal SubTotal,
    decimal TaxAmount,
    decimal DiscountAmount,
    decimal TotalAmount,
    decimal BalanceDue,
    List<InvoiceItemResponseDto> LineItems
);

public record InvoiceItemResponseDto(
    string Description,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal
);

public record ProcessPaymentDto(
    Guid InvoiceId,
    decimal AmountPaid,
    string PaymentMethod,
    string TransactionReference
);

public record PaymentResponseDto(
    Guid Id,
    decimal AmountPaid,
    string PaymentMethod,
    DateTime PaymentDate
);