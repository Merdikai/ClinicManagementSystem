using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ClinicManagementSystem.Application.Finance.DTOs;
using ClinicManagementSystem.Application.Interfaces;
using ClinicManagementSystem.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Application.Finance.Queries;

public record GetFinanceDashboardMetricsQuery : IRequest<FinanceDashboardMetricsDto>;

public class GetFinanceDashboardMetricsQueryHandler : IRequestHandler<GetFinanceDashboardMetricsQuery, FinanceDashboardMetricsDto>
{
    private readonly IClinicDbContext _context;

    public GetFinanceDashboardMetricsQueryHandler(IClinicDbContext context)
    {
        _context = context;
    }

    public async Task<FinanceDashboardMetricsDto> Handle(GetFinanceDashboardMetricsQuery request, CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;

        // 1. Total & Today Revenue from invoice payments
        var allPayments = await _context.Payments
            .Select(p => new { p.AmountPaid, p.PaymentDate, p.InvoiceId })
            .ToListAsync(cancellationToken);

        var totalRevenue = allPayments.Sum(p => p.AmountPaid);
        var todayRevenue = allPayments.Where(p => p.PaymentDate.Date >= today).Sum(p => p.AmountPaid);

        // 2. Pending Receivables (Invoices not fully paid)
        var unpaidInvoices = await _context.Invoices
            .Include(i => i.Payments)
            .Where(i => i.Status != InvoiceStatus.Paid && i.Status != InvoiceStatus.Cancelled)
            .ToListAsync(cancellationToken);

        var pendingReceivables = unpaidInvoices.Sum(i => Math.Max(0, i.TotalAmount - i.Payments.Sum(p => p.AmountPaid)));

        // 3. Expenses & Payables
        var expenses = await _context.ExpenseTransactions.ToListAsync(cancellationToken);
        var paidExpenses = expenses.Where(e => e.Status == "Paid").Sum(e => e.Amount + e.TaxAmount);
        var pendingPayables = expenses.Where(e => e.Status == "PendingApproval" || e.Status == "Approved").Sum(e => e.Amount + e.TaxAmount);

        // Payroll disbursements
        var payrollRecords = await _context.PayrollRecords.ToListAsync(cancellationToken);
        var disbursedPayroll = payrollRecords.Where(pr => pr.Status == "Disbursed").Sum(pr => pr.NetSalaryPayable);

        var totalExpenses = paidExpenses + disbursedPayroll;
        var netProfit = totalRevenue - totalExpenses;

        // 4. Claims Metrics
        var claims = await _context.InsuranceClaims.ToListAsync(cancellationToken);
        var pendingClaims = claims.Where(c => c.Status == "Submitted" || c.Status == "InReview").ToList();
        var pendingClaimsCount = pendingClaims.Count;
        var pendingClaimsAmount = pendingClaims.Sum(c => c.ClaimAmount);

        // 5. Adjustments Metrics
        var pendingAdjustmentsCount = await _context.BillingAdjustments
            .CountAsync(a => a.Status == "PendingApproval", cancellationToken);

        // 6. Petty Cash
        var latestVoucher = await _context.PettyCashVouchers
            .OrderByDescending(v => v.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        var pettyCashBalance = latestVoucher?.RemainingBalance ?? 500.00m;

        // 7. Unreconciled Receptionist Drawers
        var unreconciledDrawers = await _context.DailyCashReconciliations
            .CountAsync(r => string.IsNullOrEmpty(r.AuditStatus) || r.AuditStatus == "Pending", cancellationToken);

        // 8. Departmental Revenue breakdown
        var invoices = await _context.Invoices
            .Include(i => i.Payments)
            .Where(i => i.Payments.Any())
            .ToListAsync(cancellationToken);

        var deptRevenueList = invoices
            .GroupBy(i => string.IsNullOrWhiteSpace(i.Department) ? "General" : i.Department)
            .Select(g => new DepartmentRevenueDto
            {
                Department = g.Key,
                Revenue = g.Sum(inv => inv.Payments.Sum(p => p.AmountPaid)),
                InvoicesCount = g.Count(),
                Percentage = totalRevenue > 0 ? (double)(g.Sum(inv => inv.Payments.Sum(p => p.AmountPaid)) / totalRevenue * 100m) : 0
            })
            .OrderByDescending(d => d.Revenue)
            .ToList();

        // 9. Monthly Trend (last 6 months)
        var monthlyTrend = new List<MonthlyTrendDto>();
        for (int i = 5; i >= 0; i--)
        {
            var monthDate = DateTime.UtcNow.AddMonths(-i);
            var monthStart = new DateTime(monthDate.Year, monthDate.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var monthEnd = monthStart.AddMonths(1);

            var mRevenue = allPayments.Where(p => p.PaymentDate >= monthStart && p.PaymentDate < monthEnd).Sum(p => p.AmountPaid);
            var mExpenses = expenses.Where(e => e.Status == "Paid" && e.PaidDate.HasValue && e.PaidDate.Value >= monthStart && e.PaidDate.Value < monthEnd).Sum(e => e.Amount + e.TaxAmount)
                            + payrollRecords.Where(pr => pr.Status == "Disbursed" && pr.DisbursedAt.HasValue && pr.DisbursedAt.Value >= monthStart && pr.DisbursedAt.Value < monthEnd).Sum(pr => pr.NetSalaryPayable);

            monthlyTrend.Add(new MonthlyTrendDto
            {
                MonthName = monthDate.ToString("MMM yyyy", CultureInfo.InvariantCulture),
                Revenue = mRevenue,
                Expenses = mExpenses,
                NetIncome = mRevenue - mExpenses
            });
        }

        // 10. Recent Financial Activities
        var activities = new List<RecentFinancialActivityDto>();

        foreach (var p in allPayments.OrderByDescending(x => x.PaymentDate).Take(5))
        {
            activities.Add(new RecentFinancialActivityDto
            {
                Id = Guid.NewGuid(),
                Date = p.PaymentDate,
                Type = "PatientPayment",
                ReferenceNumber = $"PAY-{p.InvoiceId.ToString()[..8]}",
                Description = "Patient invoice settlement received",
                Amount = p.AmountPaid,
                Status = "Completed"
            });
        }

        foreach (var exp in expenses.OrderByDescending(x => x.CreatedAt).Take(4))
        {
            activities.Add(new RecentFinancialActivityDto
            {
                Id = exp.Id,
                Date = exp.CreatedAt,
                Type = "Expense",
                ReferenceNumber = exp.ExpenseNumber,
                Description = $"{exp.VendorName} - {exp.Category}",
                Amount = exp.Amount + exp.TaxAmount,
                Status = exp.Status
            });
        }

        activities = activities.OrderByDescending(a => a.Date).Take(8).ToList();

        return new FinanceDashboardMetricsDto
        {
            TotalRevenue = totalRevenue,
            TodayRevenue = todayRevenue,
            PendingReceivables = pendingReceivables,
            TotalExpenses = totalExpenses,
            NetProfit = netProfit,
            PendingClaimsCount = pendingClaimsCount,
            PendingClaimsAmount = pendingClaimsAmount,
            PendingAdjustmentsCount = pendingAdjustmentsCount,
            PendingPayablesAmount = pendingPayables,
            PettyCashCurrentBalance = pettyCashBalance,
            UnreconciledDrawersCount = unreconciledDrawers,
            DepartmentRevenue = deptRevenueList,
            MonthlyRevenueTrend = monthlyTrend,
            RecentActivities = activities
        };
    }
}
