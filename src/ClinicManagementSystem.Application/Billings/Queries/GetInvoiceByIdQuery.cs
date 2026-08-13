using ClinicManagementSystem.Application.DTOs;
using MediatR;

namespace ClinicManagementSystem.Application.Billings.Queries;

public record GetInvoiceByIdQuery(Guid Id) : IRequest<InvoiceResponseDto>;
