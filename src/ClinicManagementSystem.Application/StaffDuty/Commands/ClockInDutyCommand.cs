using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ClinicManagementSystem.Application.Interfaces;
using ClinicManagementSystem.Application.StaffDuty.DTOs;
using ClinicManagementSystem.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Application.StaffDuty.Commands;

public record ClockInDutyCommand(ClockInDutyDto Dto) : IRequest<StaffDutyShiftDto>;

public class ClockInDutyCommandHandler : IRequestHandler<ClockInDutyCommand, StaffDutyShiftDto>
{
    private readonly IClinicDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public ClockInDutyCommandHandler(IClinicDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<StaffDutyShiftDto> Handle(ClockInDutyCommand request, CancellationToken cancellationToken)
    {
        var userGuid = _currentUser.UserId;
        if (!userGuid.HasValue)
            throw new UnauthorizedAccessException("Valid user ID is required.");

        var existingActive = await _context.StaffDutyShifts
            .Where(s => s.UserId == userGuid.Value && s.IsActive)
            .ToListAsync(cancellationToken);

        foreach (var s in existingActive)
        {
            s.IsActive = false;
            s.ClockOutTimeUtc = DateTime.UtcNow;
            s.HandoverNotes = (s.HandoverNotes ?? "") + " [Auto-closed on new clock-in]";
        }

        var role = _currentUser.Roles.FirstOrDefault() ?? "Staff";

        var shift = new StaffDutyShift
        {
            Id = Guid.NewGuid(),
            UserId = userGuid.Value,
            UserName = _currentUser.UserName ?? "Staff Member",
            Role = role,
            Department = string.IsNullOrWhiteSpace(request.Dto.Department) ? "General Practice" : request.Dto.Department.Trim(),
            ShiftType = string.IsNullOrWhiteSpace(request.Dto.ShiftType) ? "Day Shift" : request.Dto.ShiftType.Trim(),
            ClockInTimeUtc = DateTime.UtcNow,
            IsActive = true,
            Notes = request.Dto.Notes
        };

        _context.StaffDutyShifts.Add(shift);
        await _context.SaveChangesAsync(cancellationToken);

        return new StaffDutyShiftDto
        {
            Id = shift.Id,
            UserId = shift.UserId,
            UserName = shift.UserName,
            Role = shift.Role,
            Department = shift.Department,
            ShiftType = shift.ShiftType,
            ClockInTimeUtc = shift.ClockInTimeUtc,
            IsActive = shift.IsActive,
            Notes = shift.Notes
        };
    }
}
