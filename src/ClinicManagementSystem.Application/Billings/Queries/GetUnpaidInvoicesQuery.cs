using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.Interfaces;
using ClinicManagementSystem.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using AutoMapper;

namespace ClinicManagementSystem.Application.Billings.Queries;

public record GetUnpaidInvoicesQuery : IRequest<IEnumerable<InvoiceResponseDto>>;

public class GetUnpaidInvoicesQueryHandler : IRequestHandler<GetUnpaidInvoicesQuery, IEnumerable<InvoiceResponseDto>>
{
    private readonly IClinicDbContext _context;
    private readonly IMapper _mapper;

    public GetUnpaidInvoicesQueryHandler(IClinicDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<IEnumerable<InvoiceResponseDto>> Handle(GetUnpaidInvoicesQuery request, CancellationToken cancellationToken)
    {
        var invoices = await _context.Invoices
            .Include(i => i.Patient)
            .Include(i => i.LineItems)
            .Where(i => i.Status == InvoiceStatus.Unpaid || i.Status == InvoiceStatus.PartiallyPaid)
            .OrderByDescending(i => i.IssueDate)
            .ToListAsync(cancellationToken);

        return _mapper.Map<IEnumerable<InvoiceResponseDto>>(invoices);
    }
}
