using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ClinicManagementSystem.Application.Common;
using ClinicManagementSystem.Application.Interfaces;
using ClinicManagementSystem.Application.Pharmacy.DTOs;
using ClinicManagementSystem.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Application.Pharmacy.Commands;

public record GetStockRequisitionsQuery(string? Status = null) : IRequest<List<PharmacyStockRequisitionDto>>;

public class GetStockRequisitionsQueryHandler : IRequestHandler<GetStockRequisitionsQuery, List<PharmacyStockRequisitionDto>>
{
    private readonly IClinicDbContext _context;

    public GetStockRequisitionsQueryHandler(IClinicDbContext context)
    {
        _context = context;
    }

    public async Task<List<PharmacyStockRequisitionDto>> Handle(GetStockRequisitionsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.PharmacyStockRequisitions
            .Include(r => r.Medicine)
            .Include(r => r.RequestedByUser)
            .Include(r => r.ApprovedByUser)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            query = query.Where(r => r.Status.ToLower() == request.Status.ToLower());
        }

        return await query
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new PharmacyStockRequisitionDto
            {
                Id = r.Id,
                RequisitionNumber = r.RequisitionNumber,
                MedicineId = r.MedicineId,
                MedicineName = r.Medicine != null ? r.Medicine.Name : "Medication",
                MedicineCode = r.Medicine != null ? r.Medicine.Code : "",
                QuantityRequested = r.QuantityRequested,
                QuantityApproved = r.QuantityApproved,
                Type = r.Type,
                Department = r.Department,
                SupplierName = r.SupplierName,
                Status = r.Status,
                RequestedByUserName = r.RequestedByUser != null
                    ? $"{r.RequestedByUser.FirstName} {r.RequestedByUser.LastName}".Trim()
                    : "Pharmacist",
                ApprovedByUserName = r.ApprovedByUser != null
                    ? $"{r.ApprovedByUser.FirstName} {r.ApprovedByUser.LastName}".Trim()
                    : null,
                CreatedAt = r.CreatedAt,
                EstimatedUnitCost = r.EstimatedUnitCost,
                TotalAmount = r.TotalAmount,
                Notes = r.Notes
            })
            .ToListAsync(cancellationToken);
    }
}

public record CreateStockRequisitionCommand(
    Guid MedicineId,
    int QuantityRequested,
    string Type,
    string Department,
    string? SupplierName,
    string? Notes,
    Guid? RequestedByUserId = null
) : IRequest<Result<PharmacyStockRequisitionDto>>;

public class CreateStockRequisitionCommandHandler : IRequestHandler<CreateStockRequisitionCommand, Result<PharmacyStockRequisitionDto>>
{
    private readonly IClinicDbContext _context;

    public CreateStockRequisitionCommandHandler(IClinicDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PharmacyStockRequisitionDto>> Handle(CreateStockRequisitionCommand request, CancellationToken cancellationToken)
    {
        var medicine = await _context.Medicines.FindAsync(new object[] { request.MedicineId }, cancellationToken);
        if (medicine == null)
            return Result<PharmacyStockRequisitionDto>.Failure("Medicine not found.", "medicine_not_found");

        if (request.QuantityRequested <= 0)
            return Result<PharmacyStockRequisitionDto>.Failure("Requested quantity must be greater than zero.", "invalid_quantity");

        var unitCost = medicine.CostPrice > 0 ? medicine.CostPrice : medicine.UnitPrice * 0.7m;
        var totalAmount = unitCost * request.QuantityRequested;
        var prefix = request.Type.Contains("Ward", StringComparison.OrdinalIgnoreCase) ? "REQ-WRD" : "PO-DIST";
        var reqNumber = $"{prefix}-{DateTime.UtcNow:yyyyMMdd}-{new Random().Next(100, 999)}";

        Guid requestingUserId = request.RequestedByUserId ?? Guid.Empty;
        if (requestingUserId == Guid.Empty)
        {
            var fallback = await _context.Users.FirstOrDefaultAsync(u => u.Username == "pharmacist.sam", cancellationToken);
            if (fallback != null) requestingUserId = fallback.Id;
        }

        var requisition = new PharmacyStockRequisition
        {
            Id = Guid.NewGuid(),
            RequisitionNumber = reqNumber,
            MedicineId = medicine.Id,
            QuantityRequested = request.QuantityRequested,
            QuantityApproved = 0,
            Type = request.Type,
            Department = request.Department,
            SupplierName = request.SupplierName ?? (request.Type.Contains("Ward", StringComparison.OrdinalIgnoreCase) ? "Inpatient Ward A" : "MedSupply Direct Ltd"),
            Status = "Pending",
            RequestedByUserId = requestingUserId,
            CreatedAt = DateTime.UtcNow,
            EstimatedUnitCost = unitCost,
            TotalAmount = totalAmount,
            Notes = request.Notes
        };

        _context.PharmacyStockRequisitions.Add(requisition);
        await _context.SaveChangesAsync(cancellationToken);

        string requestedByName = "Dispensary Pharmacist";
        if (requestingUserId != Guid.Empty)
        {
            var user = await _context.Users.FindAsync(new object[] { requestingUserId }, cancellationToken);
            if (user != null) requestedByName = $"{user.FirstName} {user.LastName}".Trim();
        }

        var dto = new PharmacyStockRequisitionDto
        {
            Id = requisition.Id,
            RequisitionNumber = requisition.RequisitionNumber,
            MedicineId = medicine.Id,
            MedicineName = medicine.Name,
            MedicineCode = medicine.Code,
            QuantityRequested = requisition.QuantityRequested,
            QuantityApproved = requisition.QuantityApproved,
            Type = requisition.Type,
            Department = requisition.Department,
            SupplierName = requisition.SupplierName,
            Status = requisition.Status,
            RequestedByUserName = requestedByName,
            CreatedAt = requisition.CreatedAt,
            EstimatedUnitCost = requisition.EstimatedUnitCost,
            TotalAmount = requisition.TotalAmount,
            Notes = requisition.Notes
        };

        return Result<PharmacyStockRequisitionDto>.Success(dto);
    }
}

public record ReviewStockRequisitionCommand(
    Guid RequisitionId,
    string Status,
    int? QuantityApproved = null,
    string? ReviewNotes = null,
    Guid? ReviewedByUserId = null
) : IRequest<Result<PharmacyStockRequisitionDto>>;

public class ReviewStockRequisitionCommandHandler : IRequestHandler<ReviewStockRequisitionCommand, Result<PharmacyStockRequisitionDto>>
{
    private readonly IClinicDbContext _context;

    public ReviewStockRequisitionCommandHandler(IClinicDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PharmacyStockRequisitionDto>> Handle(ReviewStockRequisitionCommand request, CancellationToken cancellationToken)
    {
        var requisition = await _context.PharmacyStockRequisitions
            .Include(r => r.Medicine)
            .Include(r => r.RequestedByUser)
            .FirstOrDefaultAsync(r => r.Id == request.RequisitionId, cancellationToken);

        if (requisition == null)
            return Result<PharmacyStockRequisitionDto>.Failure("Requisition not found.", "requisition_not_found");

        requisition.Status = request.Status;
        requisition.ApprovedByUserId = request.ReviewedByUserId;
        if (request.QuantityApproved.HasValue)
        {
            requisition.QuantityApproved = request.QuantityApproved.Value;
        }
        else if (request.Status.Equals("Approved", StringComparison.OrdinalIgnoreCase) || request.Status.Equals("Received", StringComparison.OrdinalIgnoreCase))
        {
            requisition.QuantityApproved = requisition.QuantityRequested;
        }

        if (!string.IsNullOrWhiteSpace(request.ReviewNotes))
        {
            requisition.Notes = string.IsNullOrWhiteSpace(requisition.Notes)
                ? request.ReviewNotes
                : $"{requisition.Notes} | Review: {request.ReviewNotes}";
        }

        // If Received / Fulfilled, update medicine stock
        if ((request.Status.Equals("Received", StringComparison.OrdinalIgnoreCase) || request.Status.Equals("Fulfilled", StringComparison.OrdinalIgnoreCase)) && requisition.Medicine != null)
        {
            var qty = requisition.QuantityApproved > 0 ? requisition.QuantityApproved : requisition.QuantityRequested;
            if (requisition.Type.Contains("Ward", StringComparison.OrdinalIgnoreCase))
            {
                requisition.Medicine.StockQuantity = Math.Max(0, requisition.Medicine.StockQuantity - qty);
            }
            else
            {
                requisition.Medicine.StockQuantity += qty;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        string approvedByName = "Dispensary Pharmacist";
        if (request.ReviewedByUserId.HasValue)
        {
            var user = await _context.Users.FindAsync(new object[] { request.ReviewedByUserId.Value }, cancellationToken);
            if (user != null) approvedByName = $"{user.FirstName} {user.LastName}".Trim();
        }

        var dto = new PharmacyStockRequisitionDto
        {
            Id = requisition.Id,
            RequisitionNumber = requisition.RequisitionNumber,
            MedicineId = requisition.MedicineId,
            MedicineName = requisition.Medicine?.Name ?? "Medication",
            MedicineCode = requisition.Medicine?.Code ?? "",
            QuantityRequested = requisition.QuantityRequested,
            QuantityApproved = requisition.QuantityApproved,
            Type = requisition.Type,
            Department = requisition.Department,
            SupplierName = requisition.SupplierName,
            Status = requisition.Status,
            RequestedByUserName = requisition.RequestedByUser != null
                ? $"{requisition.RequestedByUser.FirstName} {requisition.RequestedByUser.LastName}".Trim()
                : "Pharmacist",
            ApprovedByUserName = approvedByName,
            CreatedAt = requisition.CreatedAt,
            EstimatedUnitCost = requisition.EstimatedUnitCost,
            TotalAmount = requisition.TotalAmount,
            Notes = requisition.Notes
        };

        return Result<PharmacyStockRequisitionDto>.Success(dto);
    }
}
