using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ClinicManagementSystem.Application.Interfaces;
using ClinicManagementSystem.Application.StaffDuty.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Application.StaffDuty.Queries;

public record GetMyDutyStatusQuery : IRequest<StaffDutyShiftDto?>;

public class GetMyDutyStatusQueryHandler : IRequestHandler<GetMyDutyStatusQuery, StaffDutyShiftDto?>
{
    private readonly IClinicDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetMyDutyStatusQueryHandler(IClinicDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<StaffDutyShiftDto?> Handle(GetMyDutyStatusQuery request, CancellationToken cancellationToken)
    {
        var userGuid = _currentUser.UserId;
        if (!userGuid.HasValue)
            return null;

        var active = await _context.StaffDutyShifts
            .Where(s => s.UserId == userGuid.Value && s.IsActive)
            .OrderByDescending(s => s.ClockInTimeUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (active == null) return null;

        return new StaffDutyShiftDto
        {
            Id = active.Id,
            UserId = active.UserId,
            UserName = active.UserName,
            Role = active.Role,
            Department = active.Department,
            ShiftType = active.ShiftType,
            ClockInTimeUtc = active.ClockInTimeUtc,
            IsActive = active.IsActive,
            Notes = active.Notes,
            HandoverNotes = active.HandoverNotes
        };
    }
}
