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

public record GetBillingAdjustmentsQuery(string? Status = null) : IRequest<List<BillingAdjustmentDto>>;

public class GetBillingAdjustmentsQueryHandler : IRequestHandler<GetBillingAdjustmentsQuery, List<BillingAdjustmentDto>>
{
    private readonly IClinicDbContext _context;

    public GetBillingAdjustmentsQueryHandler(IClinicDbContext context)
    {
        _context = context;
    }

    public async Task<List<BillingAdjustmentDto>> Handle(GetBillingAdjustmentsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.BillingAdjustments
            .Include(a => a.Invoice)
                .ThenInclude(i => i.Patient)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            query = query.Where(a => a.Status == request.Status);
        }

        var adjustments = await query
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);

        // Fetch user names for requester and approver
        var userIds = adjustments
            .Where(a => a.ApprovedByUserId.HasValue)
            .Select(a => a.ApprovedByUserId!.Value)
            .Concat(adjustments.Select(a => a.RequestedByUserId))
            .Distinct()
            .ToList();

        var usersMap = await _context.Users
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => $"{u.FirstName} {u.LastName}".Trim(), cancellationToken);

        return adjustments.Select(a => new BillingAdjustmentDto
        {
            Id = a.Id,
            AdjustmentNumber = a.AdjustmentNumber,
            InvoiceId = a.InvoiceId,
            InvoiceNumber = a.Invoice?.InvoiceNumber ?? string.Empty,
            PatientName = a.Invoice?.Patient != null ? $"{a.Invoice.Patient.FirstName} {a.Invoice.Patient.LastName}" : string.Empty,
            Type = a.Type,
            Amount = a.Amount,
            Reason = a.Reason,
            ApprovedByUserId = a.ApprovedByUserId,
            ApprovedByUserName = a.ApprovedByUserId.HasValue && usersMap.TryGetValue(a.ApprovedByUserId.Value, out var appName) ? appName : null,
            ApprovedAt = a.ApprovedAt,
            Status = a.Status,
            RequestedByUserName = usersMap.TryGetValue(a.RequestedByUserId, out var reqName) ? reqName : "Staff",
            CreatedAt = a.CreatedAt
        }).ToList();
    }
}
