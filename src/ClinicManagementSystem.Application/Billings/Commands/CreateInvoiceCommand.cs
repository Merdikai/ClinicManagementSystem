using ClinicManagementSystem.Application.Common;
using ClinicManagementSystem.Application.DTOs;
using MediatR;

namespace ClinicManagementSystem.Application.Billings.Commands;

public record CreateInvoiceCommand(
    Guid PatientId,
    Guid? AppointmentId,
    decimal TaxAmount,
    decimal DiscountAmount,
    List<CreateInvoiceItemDto> Items
) : IRequest<Result<InvoiceResponseDto>>;
