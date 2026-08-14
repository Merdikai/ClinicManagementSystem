using AutoMapper;
using ClinicManagementSystem.Application.Billings.Queries;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.Exceptions;
using ClinicManagementSystem.Domain.Entities;
using ClinicManagementSystem.Domain.Interfaces;
using MediatR;

namespace ClinicManagementSystem.Application.Billings.Queries;

using ClinicManagementSystem.Application.Interfaces;

public class GetInvoiceByIdQueryHandler : IRequestHandler<GetInvoiceByIdQuery, InvoiceResponseDto>
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IMapper _mapper;
    private readonly ILinkGeneratorService _linkGenerator;

    public GetInvoiceByIdQueryHandler(IInvoiceRepository invoiceRepository, IMapper mapper, ILinkGeneratorService linkGenerator)
    {
        _invoiceRepository = invoiceRepository;
        _mapper = mapper;
        _linkGenerator = linkGenerator;
    }

    public async Task<InvoiceResponseDto> Handle(GetInvoiceByIdQuery request, CancellationToken cancellationToken)
    {
        var invoice = await _invoiceRepository.GetByIdAsync(request.Id)
            ?? throw new NotFoundException(nameof(Invoice), request.Id);

        var dto = _mapper.Map<InvoiceResponseDto>(invoice);
        dto.Links = _linkGenerator.GenerateInvoiceLinks(invoice.Id);
        return dto;
    }
}
