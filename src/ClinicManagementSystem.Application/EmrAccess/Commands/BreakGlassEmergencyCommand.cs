using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ClinicManagementSystem.Application.EmrAccess.DTOs;
using ClinicManagementSystem.Application.Interfaces;
using ClinicManagementSystem.Domain.Entities;
using MediatR;

namespace ClinicManagementSystem.Application.EmrAccess.Commands;

public record BreakGlassEmergencyCommand(BreakGlassRequestDto Dto) : IRequest<Guid>;

public class BreakGlassEmergencyCommandHandler : IRequestHandler<BreakGlassEmergencyCommand, Guid>
{
    private readonly IClinicDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public BreakGlassEmergencyCommandHandler(IClinicDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(BreakGlassEmergencyCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? Guid.Empty;
        var userRole = _currentUser.Roles.FirstOrDefault() ?? "Doctor";

        var log = new EmrAccessAuditLog
        {
            Id = Guid.NewGuid(),
            PatientId = request.Dto.PatientId,
            UserId = userId,
            UserName = _currentUser.UserName ?? "Emergency Clinician",
            UserRole = userRole,
            AccessType = "BreakGlassEmergencyOverride",
            Reason = request.Dto.Rationale,
            IsBreakGlassEmergency = true,
            Timestamp = DateTime.UtcNow
        };

        _context.EmrAccessAuditLogs.Add(log);
        await _context.SaveChangesAsync(cancellationToken);

        return log.Id;
    }
}
