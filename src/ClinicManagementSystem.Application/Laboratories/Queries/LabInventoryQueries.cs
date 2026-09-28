using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ClinicManagementSystem.Application.Common;
using ClinicManagementSystem.Application.Interfaces;
using ClinicManagementSystem.Application.Laboratories.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Application.Laboratories.Queries;

public record GetLabReagentsQuery(string? Category = null, string? Status = null, bool LowStockOnly = false, string? Search = null) : IRequest<Result<List<LabReagentDto>>>;

public class GetLabReagentsQueryHandler : IRequestHandler<GetLabReagentsQuery, Result<List<LabReagentDto>>>
{
    private readonly IClinicDbContext _context;

    public GetLabReagentsQueryHandler(IClinicDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<LabReagentDto>>> Handle(GetLabReagentsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.LabReagents.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Category) && request.Category != "ALL")
        {
            query = query.Where(r => r.Category == request.Category);
        }

        if (!string.IsNullOrWhiteSpace(request.Status) && request.Status != "ALL")
        {
            query = query.Where(r => r.Status == request.Status);
        }

        if (request.LowStockOnly)
        {
            query = query.Where(r => r.CurrentStock <= r.MinimumThreshold);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.Trim().ToLower();
            query = query.Where(r => r.Name.ToLower().Contains(s) || r.CatalogCode.ToLower().Contains(s) || r.LotNumber.ToLower().Contains(s));
        }

        var list = await query
            .OrderBy(r => r.Category)
            .ThenBy(r => r.Name)
            .Select(r => new LabReagentDto
            {
                Id = r.Id,
                Name = r.Name,
                CatalogCode = r.CatalogCode,
                Category = r.Category,
                LotNumber = r.LotNumber,
                CurrentStock = r.CurrentStock,
                MinimumThreshold = r.MinimumThreshold,
                UnitOfMeasure = r.UnitOfMeasure,
                ExpiryDate = r.ExpiryDate,
                StorageCondition = r.StorageCondition,
                Status = r.Status,
                Manufacturer = r.Manufacturer,
                UnitCost = r.UnitCost,
                LastRestockedAt = r.LastRestockedAt,
                Notes = r.Notes
            })
            .ToListAsync(cancellationToken);

        return Result<List<LabReagentDto>>.Success(list);
    }
}

public record GetLabRequisitionsQuery(string? Status = null, string? Urgency = null) : IRequest<Result<List<LabStockRequisitionDto>>>;

public class GetLabRequisitionsQueryHandler : IRequestHandler<GetLabRequisitionsQuery, Result<List<LabStockRequisitionDto>>>
{
    private readonly IClinicDbContext _context;

    public GetLabRequisitionsQueryHandler(IClinicDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<LabStockRequisitionDto>>> Handle(GetLabRequisitionsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.LabStockRequisitions.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Status) && request.Status != "ALL")
        {
            query = query.Where(r => r.Status == request.Status);
        }

        if (!string.IsNullOrWhiteSpace(request.Urgency) && request.Urgency != "ALL")
        {
            query = query.Where(r => r.Urgency == request.Urgency);
        }

        var list = await query
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new LabStockRequisitionDto
            {
                Id = r.Id,
                RequisitionNumber = r.RequisitionNumber,
                LabReagentId = r.LabReagentId,
                ItemName = r.ItemName,
                Category = r.Category,
                QuantityRequested = r.QuantityRequested,
                UnitOfMeasure = r.UnitOfMeasure,
                EstimatedUnitCost = r.EstimatedUnitCost,
                TotalEstimatedCost = r.TotalEstimatedCost,
                Urgency = r.Urgency,
                Reason = r.Reason,
                Status = r.Status,
                RequestedByUserId = r.RequestedByUserId,
                RequestedByUserName = r.RequestedByUserName,
                ApprovedByUserId = r.ApprovedByUserId,
                ApprovedByUserName = r.ApprovedByUserName,
                ReviewedAt = r.ReviewedAt,
                ReviewNotes = r.ReviewNotes,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return Result<List<LabStockRequisitionDto>>.Success(list);
    }
}
