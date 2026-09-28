using System;
using System.Threading;
using System.Threading.Tasks;
using ClinicManagementSystem.Application.Common;
using ClinicManagementSystem.Application.Interfaces;
using ClinicManagementSystem.Application.Laboratories.DTOs;
using ClinicManagementSystem.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Application.Laboratories.Commands;

public record CreateLabReagentCommand(CreateLabReagentRequest Request) : IRequest<Result<LabReagentDto>>;

public class CreateLabReagentCommandHandler : IRequestHandler<CreateLabReagentCommand, Result<LabReagentDto>>
{
    private readonly IClinicDbContext _context;

    public CreateLabReagentCommandHandler(IClinicDbContext context)
    {
        _context = context;
    }

    public async Task<Result<LabReagentDto>> Handle(CreateLabReagentCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;
        var status = req.ExpiryDate < DateTime.UtcNow ? "Expired" : (req.CurrentStock <= req.MinimumThreshold ? "LowStock" : "InStock");

        var reagent = new LabReagent
        {
            Name = req.Name,
            CatalogCode = req.CatalogCode,
            Category = req.Category,
            LotNumber = req.LotNumber,
            CurrentStock = req.CurrentStock,
            MinimumThreshold = req.MinimumThreshold,
            UnitOfMeasure = req.UnitOfMeasure,
            ExpiryDate = req.ExpiryDate,
            StorageCondition = req.StorageCondition,
            Status = status,
            Manufacturer = req.Manufacturer,
            UnitCost = req.UnitCost,
            Notes = req.Notes,
            LastRestockedAt = DateTime.UtcNow
        };

        _context.LabReagents.Add(reagent);
        await _context.SaveChangesAsync(cancellationToken);

        var dto = new LabReagentDto
        {
            Id = reagent.Id,
            Name = reagent.Name,
            CatalogCode = reagent.CatalogCode,
            Category = reagent.Category,
            LotNumber = reagent.LotNumber,
            CurrentStock = reagent.CurrentStock,
            MinimumThreshold = reagent.MinimumThreshold,
            UnitOfMeasure = reagent.UnitOfMeasure,
            ExpiryDate = reagent.ExpiryDate,
            StorageCondition = reagent.StorageCondition,
            Status = reagent.Status,
            Manufacturer = reagent.Manufacturer,
            UnitCost = reagent.UnitCost,
            LastRestockedAt = reagent.LastRestockedAt,
            Notes = reagent.Notes
        };

        return Result<LabReagentDto>.Success(dto);
    }
}

public record UpdateReagentStockCommand(Guid ReagentId, UpdateReagentStockRequest Request) : IRequest<Result<LabReagentDto>>;

public class UpdateReagentStockCommandHandler : IRequestHandler<UpdateReagentStockCommand, Result<LabReagentDto>>
{
    private readonly IClinicDbContext _context;

    public UpdateReagentStockCommandHandler(IClinicDbContext context)
    {
        _context = context;
    }

    public async Task<Result<LabReagentDto>> Handle(UpdateReagentStockCommand request, CancellationToken cancellationToken)
    {
        var reagent = await _context.LabReagents.FindAsync(new object[] { request.ReagentId }, cancellationToken);
        if (reagent == null)
            return Result<LabReagentDto>.Failure("Reagent not found");

        reagent.CurrentStock += request.Request.AdjustmentQuantity;
        if (reagent.CurrentStock < 0) reagent.CurrentStock = 0;

        if (reagent.ExpiryDate < DateTime.UtcNow)
            reagent.Status = "Expired";
        else if (reagent.CurrentStock <= reagent.MinimumThreshold)
            reagent.Status = "LowStock";
        else
            reagent.Status = "InStock";

        if (request.Request.AdjustmentQuantity > 0)
            reagent.LastRestockedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        var dto = new LabReagentDto
        {
            Id = reagent.Id,
            Name = reagent.Name,
            CatalogCode = reagent.CatalogCode,
            Category = reagent.Category,
            LotNumber = reagent.LotNumber,
            CurrentStock = reagent.CurrentStock,
            MinimumThreshold = reagent.MinimumThreshold,
            UnitOfMeasure = reagent.UnitOfMeasure,
            ExpiryDate = reagent.ExpiryDate,
            StorageCondition = reagent.StorageCondition,
            Status = reagent.Status,
            Manufacturer = reagent.Manufacturer,
            UnitCost = reagent.UnitCost,
            LastRestockedAt = reagent.LastRestockedAt,
            Notes = reagent.Notes
        };

        return Result<LabReagentDto>.Success(dto);
    }
}

public record CreateLabRequisitionCommand(CreateLabRequisitionRequest Request, Guid UserId, string UserName) : IRequest<Result<LabStockRequisitionDto>>;

public class CreateLabRequisitionCommandHandler : IRequestHandler<CreateLabRequisitionCommand, Result<LabStockRequisitionDto>>
{
    private readonly IClinicDbContext _context;

    public CreateLabRequisitionCommandHandler(IClinicDbContext context)
    {
        _context = context;
    }

    public async Task<Result<LabStockRequisitionDto>> Handle(CreateLabRequisitionCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;
        var count = await _context.LabStockRequisitions.CountAsync(cancellationToken);
        var reqNum = $"LRQ-{DateTime.UtcNow:yyyyMMdd}-{(count + 1):D4}";

        var total = req.QuantityRequested * req.EstimatedUnitCost;

        var requisition = new LabStockRequisition
        {
            RequisitionNumber = reqNum,
            LabReagentId = req.LabReagentId,
            ItemName = req.ItemName,
            Category = req.Category,
            QuantityRequested = req.QuantityRequested,
            UnitOfMeasure = req.UnitOfMeasure,
            EstimatedUnitCost = req.EstimatedUnitCost,
            TotalEstimatedCost = total,
            Urgency = req.Urgency,
            Reason = req.Reason,
            Status = "Pending",
            RequestedByUserId = request.UserId,
            RequestedByUserName = request.UserName,
            CreatedAt = DateTime.UtcNow
        };

        _context.LabStockRequisitions.Add(requisition);
        await _context.SaveChangesAsync(cancellationToken);

        var dto = new LabStockRequisitionDto
        {
            Id = requisition.Id,
            RequisitionNumber = requisition.RequisitionNumber,
            LabReagentId = requisition.LabReagentId,
            ItemName = requisition.ItemName,
            Category = requisition.Category,
            QuantityRequested = requisition.QuantityRequested,
            UnitOfMeasure = requisition.UnitOfMeasure,
            EstimatedUnitCost = requisition.EstimatedUnitCost,
            TotalEstimatedCost = requisition.TotalEstimatedCost,
            Urgency = requisition.Urgency,
            Reason = requisition.Reason,
            Status = requisition.Status,
            RequestedByUserId = requisition.RequestedByUserId,
            RequestedByUserName = requisition.RequestedByUserName,
            CreatedAt = requisition.CreatedAt
        };

        return Result<LabStockRequisitionDto>.Success(dto);
    }
}

public record ReviewLabRequisitionCommand(Guid RequisitionId, ReviewLabRequisitionRequest Request, Guid ApproverId, string ApproverName) : IRequest<Result<bool>>;

public class ReviewLabRequisitionCommandHandler : IRequestHandler<ReviewLabRequisitionCommand, Result<bool>>
{
    private readonly IClinicDbContext _context;

    public ReviewLabRequisitionCommandHandler(IClinicDbContext context)
    {
        _context = context;
    }

    public async Task<Result<bool>> Handle(ReviewLabRequisitionCommand request, CancellationToken cancellationToken)
    {
        var req = await _context.LabStockRequisitions.FindAsync(new object[] { request.RequisitionId }, cancellationToken);
        if (req == null)
            return Result<bool>.Failure("Requisition not found");

        req.Status = request.Request.Status;
        req.ApprovedByUserId = request.ApproverId;
        req.ApprovedByUserName = request.ApproverName;
        req.ReviewedAt = DateTime.UtcNow;
        req.ReviewNotes = request.Request.ReviewNotes;

        // If fulfilled and linked to reagent, restock reagent stock
        if (request.Request.Status == "Fulfilled" && req.LabReagentId.HasValue)
        {
            var reagent = await _context.LabReagents.FindAsync(new object[] { req.LabReagentId.Value }, cancellationToken);
            if (reagent != null)
            {
                reagent.CurrentStock += req.QuantityRequested;
                reagent.LastRestockedAt = DateTime.UtcNow;
                if (reagent.Status == "LowStock" && reagent.CurrentStock > reagent.MinimumThreshold)
                {
                    reagent.Status = "InStock";
                }
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }
}
