using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ClinicManagementSystem.Application.Finance.DTOs;
using ClinicManagementSystem.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Application.Finance.Queries;

public record GetPettyCashVouchersQuery(int Limit = 50) : IRequest<List<PettyCashVoucherDto>>;

public class GetPettyCashVouchersQueryHandler : IRequestHandler<GetPettyCashVouchersQuery, List<PettyCashVoucherDto>>
{
    private readonly IClinicDbContext _context;

    public GetPettyCashVouchersQueryHandler(IClinicDbContext context)
    {
        _context = context;
    }

    public async Task<List<PettyCashVoucherDto>> Handle(GetPettyCashVouchersQuery request, CancellationToken cancellationToken)
    {
        var vouchers = await _context.PettyCashVouchers
            .AsNoTracking()
            .OrderByDescending(v => v.CreatedAt)
            .Take(request.Limit)
            .ToListAsync(cancellationToken);

        return vouchers.Select(v => new PettyCashVoucherDto
        {
            Id = v.Id,
            VoucherNumber = v.VoucherNumber,
            Type = v.Type,
            Amount = v.Amount,
            Category = v.Category,
            Payee = v.Payee,
            Description = v.Description,
            DisbursedByUserId = v.DisbursedByUserId,
            DisbursedByUserName = v.DisbursedByUserName,
            RemainingBalance = v.RemainingBalance,
            CreatedAt = v.CreatedAt,
            ReceiptRef = v.ReceiptRef
        }).ToList();
    }
}
