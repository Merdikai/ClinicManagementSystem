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

        var totalPatients = await _context.Patients.CountAsync(cancellationToken);
        var todayAppointments = await _context.Appointments
            .CountAsync(a => a.ScheduledDateTime.Date == today, cancellationToken);
        var pendingAppointments = await _context.Appointments
            .CountAsync(a => a.Status == AppointmentStatus.Scheduled, cancellationToken);
        var lowStockMedicines = await _context.Medicines
            .CountAsync(m => m.StockQuantity < 10, cancellationToken);

        var todayRevenue = await _context.Invoices
            .Where(i => i.IssueDate.Date == today && i.Status != InvoiceStatus.Cancelled)
            .SumAsync(i => (decimal?)i.TotalAmount, cancellationToken) ?? 0m;

        var outstandingPayments = await _context.Invoices
            .Where(i => i.Status == InvoiceStatus.Unpaid || i.Status == InvoiceStatus.PartiallyPaid)
            .SumAsync(i => (decimal?)i.BalanceDue, cancellationToken) ?? 0m;

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
