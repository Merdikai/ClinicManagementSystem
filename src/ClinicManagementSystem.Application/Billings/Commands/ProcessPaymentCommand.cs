using ClinicManagementSystem.Application.Common;
using ClinicManagementSystem.Application.DTOs;
using MediatR;

namespace ClinicManagementSystem.Application.Billings.Commands;

public record ProcessPaymentCommand(
    Guid InvoiceId,
    decimal AmountPaid,
    string PaymentMethod,
    string TransactionReference
) : IRequest<Result<PaymentResponseDto>>;
