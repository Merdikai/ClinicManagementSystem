using ClinicManagementSystem.Application.DTOs;
using MediatR;

namespace ClinicManagementSystem.Application.Billings.Queries;

public record GetInvoicesByPatientQuery(Guid PatientId) : IRequest<IEnumerable<InvoiceResponseDto>>;
