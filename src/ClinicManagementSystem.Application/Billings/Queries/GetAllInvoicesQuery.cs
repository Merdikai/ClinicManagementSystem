using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Application.Billings.Queries;

public record GetAllInvoicesQuery(Guid? PatientId = null, string? Department = null, bool PharmacyOnly = false, bool LaboratoryOnly = false) : IRequest<IEnumerable<InvoiceResponseDto>>;

public class GetAllInvoicesQueryHandler : IRequestHandler<GetAllInvoicesQuery, IEnumerable<InvoiceResponseDto>>
{
    private readonly IClinicDbContext _context;
    private readonly IMapper _mapper;

    public GetAllInvoicesQueryHandler(IClinicDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<IEnumerable<InvoiceResponseDto>> Handle(GetAllInvoicesQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Invoices
            .Include(i => i.Patient)
            .Include(i => i.LineItems)
            .Include(i => i.Payments)
            .AsNoTracking();

        if (request.PatientId.HasValue && request.PatientId.Value != Guid.Empty)
        {
            query = query.Where(i => i.PatientId == request.PatientId.Value);
        }

        if (request.PharmacyOnly)
        {
            query = query.Where(i => i.Department == "Pharmacy" || i.InvoiceNumber.StartsWith("INV-RX"));
        }
        else if (request.LaboratoryOnly)
        {
            query = query.Where(i => i.Department == "Laboratory" || i.InvoiceNumber.StartsWith("INV-LAB"));
        }
        else if (!string.IsNullOrWhiteSpace(request.Department))
        {
            var dept = request.Department.Trim().ToLower();
            query = query.Where(i => i.Department.ToLower() == dept || 
                                     (dept == "pharmacy" && i.InvoiceNumber.StartsWith("INV-RX")) ||
                                     (dept == "laboratory" && i.InvoiceNumber.StartsWith("INV-LAB")));
        }

        var invoices = await query.OrderByDescending(i => i.IssueDate).ToListAsync(cancellationToken);
        return _mapper.Map<IEnumerable<InvoiceResponseDto>>(invoices);
    }
}
