using System;
using System.Threading;
using System.Threading.Tasks;
using ClinicManagementSystem.Application.Common;
using ClinicManagementSystem.Application.Finance.DTOs;
using ClinicManagementSystem.Application.Interfaces;
using ClinicManagementSystem.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Application.Finance.Commands;

public record CreateBillingAdjustmentCommand(
    CreateBillingAdjustmentDto Dto,
    Guid RequestedByUserId,
    string RequestedByUserName
) : IRequest<Result<BillingAdjustmentDto>>;

public class CreateBillingAdjustmentCommandHandler : IRequestHandler<CreateBillingAdjustmentCommand, Result<BillingAdjustmentDto>>
{
    private readonly IClinicDbContext _context;

    public CreateBillingAdjustmentCommandHandler(IClinicDbContext context)
    {
        _context = context;
    }

    public async Task<Result<BillingAdjustmentDto>> Handle(CreateBillingAdjustmentCommand request, CancellationToken cancellationToken)
    {
        var invoice = await _context.Invoices
            .Include(i => i.Patient)
            .FirstOrDefaultAsync(i => i.Id == request.Dto.InvoiceId, cancellationToken);

        if (invoice == null)
            return Result<BillingAdjustmentDto>.Failure("Invoice not found.", "invoice_not_found");

        if (request.Dto.Amount <= 0)
            return Result<BillingAdjustmentDto>.Failure("Adjustment amount must be greater than zero.", "invalid_amount");

        var adjNumber = $"ADJ-{DateTime.UtcNow:yyyyMMdd}-{new Random().Next(1000, 9999)}";

        var adjustment = new BillingAdjustment
        {
            Id = Guid.NewGuid(),
            AdjustmentNumber = adjNumber,
            InvoiceId = invoice.Id,
            Type = request.Dto.Type,
            Amount = request.Dto.Amount,
            Reason = request.Dto.Reason,
            Status = "PendingApproval",
            RequestedByUserId = request.RequestedByUserId,
            CreatedAt = DateTime.UtcNow
        };

        _context.BillingAdjustments.Add(adjustment);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<BillingAdjustmentDto>.Success(new BillingAdjustmentDto
        {
            Id = adjustment.Id,
            AdjustmentNumber = adjustment.AdjustmentNumber,
            InvoiceId = adjustment.InvoiceId,
            InvoiceNumber = invoice.InvoiceNumber,
            PatientName = invoice.Patient != null ? $"{invoice.Patient.FirstName} {invoice.Patient.LastName}" : string.Empty,
            Type = adjustment.Type,
            Amount = adjustment.Amount,
            Reason = adjustment.Reason,
            Status = adjustment.Status,
            RequestedByUserName = request.RequestedByUserName,
            CreatedAt = adjustment.CreatedAt
        });
    }
}

public record ReviewBillingAdjustmentCommand(
    Guid AdjustmentId,
    ReviewBillingAdjustmentDto Dto,
    Guid ApproverUserId,
    string ApproverUserName
) : IRequest<Result<BillingAdjustmentDto>>;

public class ReviewBillingAdjustmentCommandHandler : IRequestHandler<ReviewBillingAdjustmentCommand, Result<BillingAdjustmentDto>>
{
    private readonly IClinicDbContext _context;

    public ReviewBillingAdjustmentCommandHandler(IClinicDbContext context)
    {
        _context = context;
    }

    public async Task<Result<BillingAdjustmentDto>> Handle(ReviewBillingAdjustmentCommand request, CancellationToken cancellationToken)
    {
        var adjustment = await _context.BillingAdjustments
            .Include(a => a.Invoice)
                .ThenInclude(i => i.Patient)
            .FirstOrDefaultAsync(a => a.Id == request.AdjustmentId, cancellationToken);

        if (adjustment == null)
            return Result<BillingAdjustmentDto>.Failure("Billing adjustment not found.", "adjustment_not_found");

        adjustment.Status = request.Dto.Status;
        adjustment.ApprovedByUserId = request.ApproverUserId;
        adjustment.ApprovedAt = DateTime.UtcNow;

        if (request.Dto.Status == "Approved" && adjustment.Invoice != null)
        {
            var inv = adjustment.Invoice;
            if (adjustment.Type == "Discount" || adjustment.Type == "CourtesyWaiver" || adjustment.Type == "BadDebtWriteOff")
            {
                inv.DiscountAmount += adjustment.Amount;
                inv.TotalAmount = Math.Max(0, inv.SubTotal + inv.TaxAmount - inv.DiscountAmount);
                inv.BillingNotes = string.IsNullOrWhiteSpace(inv.BillingNotes)
                    ? $"Approved {adjustment.Type} of {adjustment.Amount:C}: {adjustment.Reason}"
                    : $"{inv.BillingNotes}; Approved {adjustment.Type} of {adjustment.Amount:C}";
            }
            else if (adjustment.Type == "Refund")
            {
                // Record negative payment/refund
                var refundPayment = new Payment
                {
                    Id = Guid.NewGuid(),
                    InvoiceId = inv.Id,
                    AmountPaid = -adjustment.Amount,
                    PaymentDate = DateTime.UtcNow,
                    PaymentMethod = "Refund (Reversal)",
                    TransactionReference = $"REF-{adjustment.AdjustmentNumber}"
                };
                _context.Payments.Add(refundPayment);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Result<BillingAdjustmentDto>.Success(new BillingAdjustmentDto
        {
            Id = adjustment.Id,
            AdjustmentNumber = adjustment.AdjustmentNumber,
            InvoiceId = adjustment.InvoiceId,
            InvoiceNumber = adjustment.Invoice?.InvoiceNumber ?? string.Empty,
            PatientName = adjustment.Invoice?.Patient != null ? $"{adjustment.Invoice.Patient.FirstName} {adjustment.Invoice.Patient.LastName}" : string.Empty,
            Type = adjustment.Type,
            Amount = adjustment.Amount,
            Reason = adjustment.Reason,
            ApprovedByUserId = adjustment.ApprovedByUserId,
            ApprovedByUserName = request.ApproverUserName,
            ApprovedAt = adjustment.ApprovedAt,
            Status = adjustment.Status,
            RequestedByUserName = "Staff",
            CreatedAt = adjustment.CreatedAt
        });
    }
}
