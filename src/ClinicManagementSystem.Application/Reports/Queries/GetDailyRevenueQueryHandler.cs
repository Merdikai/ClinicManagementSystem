using ClinicManagementSystem.Application.Reports.DTOs;
using ClinicManagementSystem.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Application.Reports.Queries;

public class GetDailyRevenueQueryHandler : IRequestHandler<GetDailyRevenueQuery, DailyRevenueReportDto>
{
    private readonly IClinicDbContext _context;

    public GetDailyRevenueQueryHandler(IClinicDbContext context)
    {
        _context = context;
    }

    public async Task<DailyRevenueReportDto> Handle(GetDailyRevenueQuery request, CancellationToken cancellationToken)
    {
        var start = request.Date.Date;
        var end = start.AddDays(1);

        var invoices = await _context.Invoices
            .Where(i => i.IssueDate >= start && i.IssueDate < end && i.Status != Domain.Enums.InvoiceStatus.Cancelled)
            .ToListAsync(cancellationToken);

        var appointments = await _context.Appointments
            .Where(a => a.ScheduledDateTime >= start && a.ScheduledDateTime < end && !a.IsDeleted)
            .CountAsync(cancellationToken);

        var patients = await _context.Patients
            .Where(p => p.RegisteredAt >= start && p.RegisteredAt < end)
            .CountAsync(cancellationToken);

        return new DailyRevenueReportDto(
            request.Date,
            patients,
            appointments,
            invoices.Sum(i => i.TotalAmount),
            invoices.Sum(i => i.TaxAmount),
            invoices.Sum(i => i.DiscountAmount)
        );
    }
}
