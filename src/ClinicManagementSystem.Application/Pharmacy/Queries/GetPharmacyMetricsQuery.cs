using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ClinicManagementSystem.Application.Interfaces;
using ClinicManagementSystem.Application.Pharmacy.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Application.Pharmacy.Queries;

public record GetPharmacyMetricsQuery() : IRequest<PharmacyMetricsDto>;

public class GetPharmacyMetricsQueryHandler : IRequestHandler<GetPharmacyMetricsQuery, PharmacyMetricsDto>
{
    private readonly IClinicDbContext _context;

    public GetPharmacyMetricsQueryHandler(IClinicDbContext context)
    {
        _context = context;
    }

    public async Task<PharmacyMetricsDto> Handle(GetPharmacyMetricsQuery request, CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;
        var nearExpiryThreshold = DateTime.UtcNow.AddDays(60);

        var pendingRx = await _context.Prescriptions
            .CountAsync(p => p.Status == "Pending", cancellationToken);

        var dispensedToday = await _context.Prescriptions
            .CountAsync(p => p.DispensedAt.HasValue && p.DispensedAt.Value >= today, cancellationToken);

        var lowStock = await _context.Medicines
            .CountAsync(m => m.IsActive && m.StockQuantity <= m.ReorderLevel, cancellationToken);

        var nearExpiry = await _context.Medicines
            .CountAsync(m => m.IsActive && m.ExpiryDate.HasValue && m.ExpiryDate.Value <= nearExpiryThreshold, cancellationToken);

        var controlledCount = await _context.Medicines
            .CountAsync(m => m.IsActive && m.IsControlledSubstance, cancellationToken);

        var todayRevenue = await _context.Prescriptions
            .Where(p => p.DispensedAt.HasValue && p.DispensedAt.Value >= today)
            .SelectMany(p => p.PrescriptionItems)
            .SumAsync(pi => (decimal?)((pi.DispensedQuantity > 0 ? pi.DispensedQuantity : pi.Quantity) * pi.UnitPrice), cancellationToken) ?? 0m;

        return new PharmacyMetricsDto
        {
            PendingPrescriptionsCount = pendingRx,
            DispensedTodayCount = dispensedToday,
            LowStockMedicinesCount = lowStock,
            NearExpiryMedicinesCount = nearExpiry,
            ControlledSubstancesCount = controlledCount,
            TodayDispensaryRevenue = todayRevenue
        };
    }
}
