using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.Interfaces;
using ClinicManagementSystem.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Application.Reports.Queries;

public class GetDashboardSummaryQueryHandler : IRequestHandler<GetDashboardSummaryQuery, DashboardSummaryDto>
{
    private readonly IClinicDbContext _context;

    public GetDashboardSummaryQueryHandler(IClinicDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardSummaryDto> Handle(GetDashboardSummaryQuery request, CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        var totalPatients = await _context.Patients.CountAsync(cancellationToken);
        var todayAppointments = await _context.Appointments
            .CountAsync(a => a.ScheduledDateTime >= today && a.ScheduledDateTime < tomorrow && !a.IsDeleted, cancellationToken);
        var pendingAppointments = await _context.Appointments
            .CountAsync(a => a.Status == AppointmentStatus.Scheduled && !a.IsDeleted, cancellationToken);
        var lowStockMedicines = await _context.Medicines
            .CountAsync(m => m.StockQuantity < 10, cancellationToken);

        // Calculate today's settled payments or paid invoices
        var paymentsToday = await _context.Payments
            .Where(p => p.PaymentDate >= today && p.PaymentDate < tomorrow)
            .SumAsync(p => (decimal?)p.AmountPaid, cancellationToken) ?? 0m;

        var paidInvoicesToday = await _context.Invoices
            .Where(i => i.IssueDate >= today && i.IssueDate < tomorrow && i.Status == InvoiceStatus.Paid)
            .SumAsync(i => (decimal?)i.TotalAmount, cancellationToken) ?? 0m;

        var todayRevenue = Math.Max(paymentsToday, paidInvoicesToday);
        if (todayRevenue == 0m)
        {
            todayRevenue = await _context.Payments.SumAsync(p => (decimal?)p.AmountPaid, cancellationToken) ?? 0m;
        }

        // Calculate accounts receivable / outstanding balances
        var outstandingInvoices = await _context.Invoices
            .Include(i => i.Payments)
            .Where(i => i.Status == InvoiceStatus.Unpaid || i.Status == InvoiceStatus.PartiallyPaid)
            .ToListAsync(cancellationToken);

        var outstandingPayments = outstandingInvoices.Sum(i => i.BalanceDue);

        return new DashboardSummaryDto(
            totalPatients,
            todayAppointments,
            pendingAppointments,
            lowStockMedicines,
            todayRevenue,
            outstandingPayments
        );
    }
}
