using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ClinicManagementSystem.Application.Interfaces;
using ClinicManagementSystem.Application.StaffDuty.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Application.StaffDuty.Queries;

public record GetActiveStaffDutyQuery : IRequest<List<StaffDutyShiftDto>>;

public class GetActiveStaffDutyQueryHandler : IRequestHandler<GetActiveStaffDutyQuery, List<StaffDutyShiftDto>>
{
    private readonly IClinicDbContext _context;

    public GetActiveStaffDutyQueryHandler(IClinicDbContext context)
    {
        _context = context;
    }

    public async Task<List<StaffDutyShiftDto>> Handle(GetActiveStaffDutyQuery request, CancellationToken cancellationToken)
    {
        return await _context.StaffDutyShifts
            .Where(s => s.IsActive)
            .OrderBy(s => s.Department)
            .ThenBy(s => s.UserName)
            .Select(s => new StaffDutyShiftDto
            {
                Id = s.Id,
                UserId = s.UserId,
                UserName = s.UserName,
                Role = s.Role,
                Department = s.Department,
                ShiftType = s.ShiftType,
                ClockInTimeUtc = s.ClockInTimeUtc,
                IsActive = s.IsActive,
                Notes = s.Notes
            })
            .ToListAsync(cancellationToken);
    }
}
