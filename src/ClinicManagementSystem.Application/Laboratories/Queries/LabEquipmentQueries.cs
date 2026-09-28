using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ClinicManagementSystem.Application.Common;
using ClinicManagementSystem.Application.Interfaces;
using ClinicManagementSystem.Application.Laboratories.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Application.Laboratories.Queries;

public record GetLabEquipmentLogsQuery(string? EquipmentName = null, string? LogType = null) : IRequest<Result<List<LabEquipmentLogDto>>>;

public class GetLabEquipmentLogsQueryHandler : IRequestHandler<GetLabEquipmentLogsQuery, Result<List<LabEquipmentLogDto>>>
{
    private readonly IClinicDbContext _context;

    public GetLabEquipmentLogsQueryHandler(IClinicDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<LabEquipmentLogDto>>> Handle(GetLabEquipmentLogsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.LabEquipmentLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.EquipmentName))
        {
            query = query.Where(l => l.EquipmentName.ToLower().Contains(request.EquipmentName.Trim().ToLower()));
        }

        if (!string.IsNullOrWhiteSpace(request.LogType))
        {
            query = query.Where(l => l.LogType == request.LogType);
        }

        var list = await query
            .OrderByDescending(l => l.PerformedAt)
            .Select(l => new LabEquipmentLogDto
            {
                Id = l.Id,
                EquipmentName = l.EquipmentName,
                EquipmentModel = l.EquipmentModel,
                SerialNumber = l.SerialNumber,
                Location = l.Location,
                LogType = l.LogType,
                ControlLotNumber = l.ControlLotNumber,
                StandardDeviationScore = l.StandardDeviationScore,
                Status = l.Status,
                Findings = l.Findings,
                CorrectiveAction = l.CorrectiveAction,
                PerformedByUserId = l.PerformedByUserId,
                PerformedByUserName = l.PerformedByUserName,
                PerformedAt = l.PerformedAt,
                NextMaintenanceDueDate = l.NextMaintenanceDueDate,
                CreatedAt = l.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return Result<List<LabEquipmentLogDto>>.Success(list);
    }
}
