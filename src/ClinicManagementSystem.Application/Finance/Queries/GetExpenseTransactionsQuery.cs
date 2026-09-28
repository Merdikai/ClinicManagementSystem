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

public record GetExpenseTransactionsQuery(string? Category = null, string? Status = null) : IRequest<List<ExpenseTransactionDto>>;

public class GetExpenseTransactionsQueryHandler : IRequestHandler<GetExpenseTransactionsQuery, List<ExpenseTransactionDto>>
{
    private readonly IClinicDbContext _context;

    public GetExpenseTransactionsQueryHandler(IClinicDbContext context)
    {
        _context = context;
    }

    public async Task<List<ExpenseTransactionDto>> Handle(GetExpenseTransactionsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.ExpenseTransactions
            .Include(e => e.PharmacyStockRequisition)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Category))
        {
            query = query.Where(e => e.Category == request.Category);
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            query = query.Where(e => e.Status == request.Status);
        }

        var expenses = await query
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync(cancellationToken);

        // Fetch user names for RecordedBy and ApprovedBy
        var userIds = expenses.Select(e => e.RecordedByUserId)
            .Concat(expenses.Where(e => e.ApprovedByUserId.HasValue).Select(e => e.ApprovedByUserId!.Value))
            .Distinct()
            .ToList();

        var usersMap = await _context.Users
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => $"{u.FirstName} {u.LastName}", cancellationToken);

        return expenses.Select(e => new ExpenseTransactionDto
        {
            Id = e.Id,
            ExpenseNumber = e.ExpenseNumber,
            Category = e.Category,
            VendorName = e.VendorName,
            InvoiceReference = e.InvoiceReference,
            Description = e.Description,
            Amount = e.Amount,
            TaxAmount = e.TaxAmount,
            DueDate = e.DueDate,
            PaidDate = e.PaidDate,
            PaymentMethod = e.PaymentMethod,
            Status = e.Status,
            PharmacyStockRequisitionId = e.PharmacyStockRequisitionId,
            PharmacyRequisitionNumber = e.PharmacyStockRequisition?.RequisitionNumber,
            RecordedByUserName = usersMap.TryGetValue(e.RecordedByUserId, out var recName) ? recName : "Staff",
            ApprovedByUserName = e.ApprovedByUserId.HasValue && usersMap.TryGetValue(e.ApprovedByUserId.Value, out var appName) ? appName : null,
            CreatedAt = e.CreatedAt,
            Notes = e.Notes
        }).ToList();
    }
}
