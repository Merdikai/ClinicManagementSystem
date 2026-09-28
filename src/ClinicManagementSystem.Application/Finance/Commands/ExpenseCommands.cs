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

public record CreateExpenseTransactionCommand(
    CreateExpenseTransactionDto Dto,
    Guid RecordedByUserId,
    string RecordedByUserName
) : IRequest<Result<ExpenseTransactionDto>>;

public class CreateExpenseTransactionCommandHandler : IRequestHandler<CreateExpenseTransactionCommand, Result<ExpenseTransactionDto>>
{
    private readonly IClinicDbContext _context;

    public CreateExpenseTransactionCommandHandler(IClinicDbContext context)
    {
        _context = context;
    }

    public async Task<Result<ExpenseTransactionDto>> Handle(CreateExpenseTransactionCommand request, CancellationToken cancellationToken)
    {
        if (request.Dto.Amount <= 0)
            return Result<ExpenseTransactionDto>.Failure("Expense amount must be greater than zero.", "invalid_amount");

        var expNumber = $"EXP-{DateTime.UtcNow:yyyyMMdd}-{new Random().Next(1000, 9999)}";

        var expense = new ExpenseTransaction
        {
            Id = Guid.NewGuid(),
            ExpenseNumber = expNumber,
            Category = request.Dto.Category,
            VendorName = request.Dto.VendorName,
            InvoiceReference = request.Dto.InvoiceReference,
            Description = request.Dto.Description,
            Amount = request.Dto.Amount,
            TaxAmount = request.Dto.TaxAmount,
            DueDate = request.Dto.DueDate,
            PaymentMethod = request.Dto.PaymentMethod,
            Status = "PendingApproval",
            PharmacyStockRequisitionId = request.Dto.PharmacyStockRequisitionId,
            RecordedByUserId = request.RecordedByUserId,
            CreatedAt = DateTime.UtcNow,
            Notes = request.Dto.Notes
        };

        _context.ExpenseTransactions.Add(expense);
        await _context.SaveChangesAsync(cancellationToken);

        string? reqNumber = null;
        if (expense.PharmacyStockRequisitionId.HasValue)
        {
            var req = await _context.PharmacyStockRequisitions.FindAsync(new object[] { expense.PharmacyStockRequisitionId.Value }, cancellationToken);
            reqNumber = req?.RequisitionNumber;
        }

        return Result<ExpenseTransactionDto>.Success(new ExpenseTransactionDto
        {
            Id = expense.Id,
            ExpenseNumber = expense.ExpenseNumber,
            Category = expense.Category,
            VendorName = expense.VendorName,
            InvoiceReference = expense.InvoiceReference,
            Description = expense.Description,
            Amount = expense.Amount,
            TaxAmount = expense.TaxAmount,
            DueDate = expense.DueDate,
            PaymentMethod = expense.PaymentMethod,
            Status = expense.Status,
            PharmacyStockRequisitionId = expense.PharmacyStockRequisitionId,
            PharmacyRequisitionNumber = reqNumber,
            RecordedByUserName = request.RecordedByUserName,
            CreatedAt = expense.CreatedAt,
            Notes = expense.Notes
        });
    }
}

public record SettleExpenseTransactionCommand(
    Guid ExpenseId,
    SettleExpenseTransactionDto Dto,
    Guid ApproverUserId,
    string ApproverUserName
) : IRequest<Result<ExpenseTransactionDto>>;

public class SettleExpenseTransactionCommandHandler : IRequestHandler<SettleExpenseTransactionCommand, Result<ExpenseTransactionDto>>
{
    private readonly IClinicDbContext _context;

    public SettleExpenseTransactionCommandHandler(IClinicDbContext context)
    {
        _context = context;
    }

    public async Task<Result<ExpenseTransactionDto>> Handle(SettleExpenseTransactionCommand request, CancellationToken cancellationToken)
    {
        var expense = await _context.ExpenseTransactions
            .Include(e => e.PharmacyStockRequisition)
            .FirstOrDefaultAsync(e => e.Id == request.ExpenseId, cancellationToken);

        if (expense == null)
            return Result<ExpenseTransactionDto>.Failure("Expense transaction not found.", "expense_not_found");

        expense.Status = "Paid";
        expense.PaidDate = DateTime.UtcNow;
        expense.PaymentMethod = request.Dto.PaymentMethod;
        expense.ApprovedByUserId = request.ApproverUserId;

        if (!string.IsNullOrWhiteSpace(request.Dto.Notes))
        {
            expense.Notes = string.IsNullOrWhiteSpace(expense.Notes)
                ? request.Dto.Notes
                : $"{expense.Notes} | Settled: {request.Dto.Notes}";
        }

        // If this expense is linked to a Pharmacy Stock Requisition, fulfill the PO status!
        if (expense.PharmacyStockRequisition != null && expense.PharmacyStockRequisition.Status != "Fulfilled")
        {
            expense.PharmacyStockRequisition.Status = "Fulfilled";
            expense.PharmacyStockRequisition.Notes = string.IsNullOrWhiteSpace(expense.PharmacyStockRequisition.Notes)
                ? $"Paid via Finance AP ({expense.ExpenseNumber})"
                : $"{expense.PharmacyStockRequisition.Notes}; Paid via Finance AP ({expense.ExpenseNumber})";
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Result<ExpenseTransactionDto>.Success(new ExpenseTransactionDto
        {
            Id = expense.Id,
            ExpenseNumber = expense.ExpenseNumber,
            Category = expense.Category,
            VendorName = expense.VendorName,
            InvoiceReference = expense.InvoiceReference,
            Description = expense.Description,
            Amount = expense.Amount,
            TaxAmount = expense.TaxAmount,
            DueDate = expense.DueDate,
            PaidDate = expense.PaidDate,
            PaymentMethod = expense.PaymentMethod,
            Status = expense.Status,
            PharmacyStockRequisitionId = expense.PharmacyStockRequisitionId,
            PharmacyRequisitionNumber = expense.PharmacyStockRequisition?.RequisitionNumber,
            RecordedByUserName = "Staff",
            ApprovedByUserName = request.ApproverUserName,
            CreatedAt = expense.CreatedAt,
            Notes = expense.Notes
        });
    }
}
