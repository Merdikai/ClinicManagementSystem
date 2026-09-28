using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ClinicManagementSystem.Application.Interfaces;
using ClinicManagementSystem.Application.StaffDuty.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Application.StaffDuty.Commands;

public record ClockOutDutyCommand(ClockOutDutyDto Dto) : IRequest<StaffDutyShiftDto?>;

public class ClockOutDutyCommandHandler : IRequestHandler<ClockOutDutyCommand, StaffDutyShiftDto?>
{
    private readonly IClinicDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public ClockOutDutyCommandHandler(IClinicDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<StaffDutyShiftDto?> Handle(ClockOutDutyCommand request, CancellationToken cancellationToken)
    {
        var userGuid = _currentUser.UserId;
        if (!userGuid.HasValue)
            throw new UnauthorizedAccessException("Valid user ID is required.");

        var activeShift = await _context.StaffDutyShifts
            .Where(s => s.UserId == userGuid.Value && s.IsActive)
            .OrderByDescending(s => s.ClockInTimeUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (activeShift == null)
            return null;

        activeShift.IsActive = false;
        activeShift.ClockOutTimeUtc = DateTime.UtcNow;
        activeShift.HandoverNotes = request.Dto.HandoverNotes;

        await _context.SaveChangesAsync(cancellationToken);

        return new StaffDutyShiftDto
        {
            Id = activeShift.Id,
            UserId = activeShift.UserId,
            UserName = activeShift.UserName,
            Role = activeShift.Role,
            Department = activeShift.Department,
            ShiftType = activeShift.ShiftType,
            ClockInTimeUtc = activeShift.ClockInTimeUtc,
            ClockOutTimeUtc = activeShift.ClockOutTimeUtc,
            IsActive = activeShift.IsActive,
            Notes = activeShift.Notes,
            HandoverNotes = activeShift.HandoverNotes
        };
    }
}
