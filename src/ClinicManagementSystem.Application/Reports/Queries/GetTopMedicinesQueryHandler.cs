using ClinicManagementSystem.Application.Reports.DTOs;
using ClinicManagementSystem.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Application.Reports.Queries;

public class GetTopMedicinesQueryHandler : IRequestHandler<GetTopMedicinesQuery, IEnumerable<TopMedicineDto>>
{
    private readonly IClinicDbContext _context;

    public GetTopMedicinesQueryHandler(IClinicDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<TopMedicineDto>> Handle(GetTopMedicinesQuery request, CancellationToken cancellationToken)
    {
        var items = await _context.PrescriptionItems
            .Include(pi => pi.Medicine)
            .Where(pi => pi.Medicine != null)
            .Select(pi => new {
                pi.Medicine.Name,
                pi.Medicine.Code,
                pi.Quantity,
                pi.UnitPrice
            })
            .ToListAsync(cancellationToken);

        return items
            .GroupBy(pi => new { pi.Name, pi.Code })
            .Select(g => new TopMedicineDto(
                g.Key.Name,
                g.Key.Code,
                g.Sum(pi => pi.Quantity),
                g.Sum(pi => pi.Quantity * pi.UnitPrice)
            ))
            .OrderByDescending(m => m.TotalRevenue)
            .Take(request.Count)
            .ToList();
    }
}
