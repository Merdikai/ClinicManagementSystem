using ClinicManagementSystem.Application.DTOs;

namespace ClinicManagementSystem.Application.Interfaces;

public interface IBillingService
{
    Task<InvoiceResponseDto> CreateInvoiceAsync(CreateInvoiceDto dto);
    Task<InvoiceResponseDto?> GetInvoiceByIdAsync(Guid id);
    Task<IEnumerable<InvoiceResponseDto>> GetInvoicesByPatientAsync(Guid patientId);
    Task<PaymentResponseDto> ProcessPaymentAsync(ProcessPaymentDto dto);
}