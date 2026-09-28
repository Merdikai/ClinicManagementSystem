using System;
using System.Threading;
using System.Threading.Tasks;
using ClinicManagementSystem.Application.Common;
using ClinicManagementSystem.Application.Interfaces;
using ClinicManagementSystem.Application.Laboratories.DTOs;
using ClinicManagementSystem.Domain.Entities;
using MediatR;

namespace ClinicManagementSystem.Application.Laboratories.Commands;

public record CreateLabEquipmentLogCommand(CreateLabEquipmentLogRequest Request, Guid CurrentUserId, string CurrentUserName)
    : IRequest<Result<LabEquipmentLogDto>>;

public class CreateLabEquipmentLogCommandHandler : IRequestHandler<CreateLabEquipmentLogCommand, Result<LabEquipmentLogDto>>
{
    private readonly IClinicDbContext _context;

    public CreateLabEquipmentLogCommandHandler(IClinicDbContext context)
    {
        _context = context;
    }

    public async Task<Result<LabEquipmentLogDto>> Handle(CreateLabEquipmentLogCommand command, CancellationToken cancellationToken)
    {
        var req = command.Request;

        var log = new LabEquipmentLog
        {
            EquipmentName = req.EquipmentName,
            EquipmentModel = req.EquipmentModel,
            SerialNumber = req.SerialNumber,
            Location = req.Location,
            LogType = req.LogType,
            ControlLotNumber = req.ControlLotNumber,
            StandardDeviationScore = req.StandardDeviationScore,
            Status = req.Status,
            Findings = req.Findings,
            CorrectiveAction = req.CorrectiveAction,
            PerformedByUserId = command.CurrentUserId,
            PerformedByUserName = command.CurrentUserName,
            PerformedAt = DateTime.UtcNow,
            NextMaintenanceDueDate = req.NextMaintenanceDueDate,
            CreatedAt = DateTime.UtcNow
        };

        _context.LabEquipmentLogs.Add(log);
        await _context.SaveChangesAsync(cancellationToken);

        var dto = new LabEquipmentLogDto
        {
            Id = log.Id,
            EquipmentName = log.EquipmentName,
            EquipmentModel = log.EquipmentModel,
            SerialNumber = log.SerialNumber,
            Location = log.Location,
            LogType = log.LogType,
            ControlLotNumber = log.ControlLotNumber,
            StandardDeviationScore = log.StandardDeviationScore,
            Status = log.Status,
            Findings = log.Findings,
            CorrectiveAction = log.CorrectiveAction,
            PerformedByUserId = log.PerformedByUserId,
            PerformedByUserName = log.PerformedByUserName,
            PerformedAt = log.PerformedAt,
            NextMaintenanceDueDate = log.NextMaintenanceDueDate,
            CreatedAt = log.CreatedAt
        };

        return Result<LabEquipmentLogDto>.Success(dto);
    }
}
