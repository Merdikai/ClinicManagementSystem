using AutoMapper;
using ClinicManagementSystem.Application.Billings.Queries;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Domain.Interfaces;
using MediatR;

namespace ClinicManagementSystem.Application.Billings.Queries;

public class GetInvoicesByPatientQueryHandler : IRequestHandler<GetInvoicesByPatientQuery, IEnumerable<InvoiceResponseDto>>
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IMapper _mapper;

    public GetInvoicesByPatientQueryHandler(IInvoiceRepository invoiceRepository, IMapper mapper)
    {
        _invoiceRepository = invoiceRepository;
        _mapper = mapper;
    }

    public async Task<IEnumerable<InvoiceResponseDto>> Handle(GetInvoicesByPatientQuery request, CancellationToken cancellationToken)
    {
        var invoices = await _invoiceRepository.GetByPatientIdAsync(request.PatientId);
        return _mapper.Map<IEnumerable<InvoiceResponseDto>>(invoices);
    }
}
