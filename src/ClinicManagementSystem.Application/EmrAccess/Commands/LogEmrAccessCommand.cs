using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ClinicManagementSystem.Application.EmrAccess.DTOs;
using ClinicManagementSystem.Application.Interfaces;
using ClinicManagementSystem.Domain.Entities;
using MediatR;

namespace ClinicManagementSystem.Application.EmrAccess.Commands;

public record LogEmrAccessCommand(LogEmrAccessDto Dto) : IRequest<Guid>;

public class LogEmrAccessCommandHandler : IRequestHandler<LogEmrAccessCommand, Guid>
{
    private readonly IClinicDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public LogEmrAccessCommandHandler(IClinicDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(LogEmrAccessCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? Guid.Empty;
        var userRole = _currentUser.Roles.FirstOrDefault() ?? "Staff";

        var log = new EmrAccessAuditLog
        {
            Id = Guid.NewGuid(),
            PatientId = request.Dto.PatientId,
            UserId = userId,
            UserName = _currentUser.UserName ?? "User",
            UserRole = userRole,
            AccessType = request.Dto.AccessType,
            Reason = request.Dto.ActionDetail,
            IsBreakGlassEmergency = false,
            Timestamp = DateTime.UtcNow
        };

        _context.EmrAccessAuditLogs.Add(log);
        await _context.SaveChangesAsync(cancellationToken);

        return log.Id;
    }
}
