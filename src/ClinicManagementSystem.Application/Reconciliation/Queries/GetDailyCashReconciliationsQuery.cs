using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Application.Reconciliation.Queries;

public record GetDailyCashReconciliationsQuery(int Limit = 30) : IRequest<List<DailyCashReconciliationResponseDto>>;

public class GetDailyCashReconciliationsQueryHandler : IRequestHandler<GetDailyCashReconciliationsQuery, List<DailyCashReconciliationResponseDto>>
{
    private readonly IClinicDbContext _context;

    public GetDailyCashReconciliationsQueryHandler(IClinicDbContext context)
    {
        _context = context;
    }

    public async Task<List<DailyCashReconciliationResponseDto>> Handle(GetDailyCashReconciliationsQuery request, CancellationToken cancellationToken)
    {
        var logs = await _context.DailyCashReconciliations
            .Include(r => r.ReceptionistUser)
            .OrderByDescending(r => r.ReconciliationDate)
            .Take(request.Limit)
            .ToListAsync(cancellationToken);

        return logs.Select(r => new DailyCashReconciliationResponseDto
        {
            Id = r.Id,
            ReceptionistUserId = r.ReceptionistUserId,
            ReceptionistName = r.ReceptionistUser != null ? $"{r.ReceptionistUser.FirstName} {r.ReceptionistUser.LastName}" : "Front Desk",
            ReconciliationDate = r.ReconciliationDate,
            Shift = r.Shift,
            CashCollected = r.CashCollected,
            CardCollected = r.CardCollected,
            DigitalCollected = r.DigitalCollected,
            TotalCollected = r.TotalCollected,
            ClosingCashCounted = r.ClosingCashCounted,
            DiscrepancyAmount = r.DiscrepancyAmount,
            InvoicesSettledCount = r.InvoicesSettledCount,
            Status = r.Status,
            Notes = r.Notes,
            CreatedAt = r.CreatedAt
        }).ToList();
    }
}
