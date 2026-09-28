using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ClinicManagementSystem.Application.DoctorDelegations.DTOs;
using ClinicManagementSystem.Application.Interfaces;
using ClinicManagementSystem.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Application.DoctorDelegations.Commands;

public record CreateDoctorDelegationCommand(CreateDoctorDelegationDto Dto) : IRequest<DoctorDelegationDto>;

public class CreateDoctorDelegationCommandHandler : IRequestHandler<CreateDoctorDelegationCommand, DoctorDelegationDto>
{
    private readonly IClinicDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public CreateDoctorDelegationCommandHandler(IClinicDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<DoctorDelegationDto> Handle(CreateDoctorDelegationCommand request, CancellationToken cancellationToken)
    {
        var userGuid = _currentUser.UserId;
        if (!userGuid.HasValue)
            throw new UnauthorizedAccessException("Valid user ID required.");

        var isSystemAdmin = _currentUser.IsAdmin;
        var delegatorId = userGuid.Value;
        var delegatorName = _currentUser.UserName ?? "Attending Doctor";

        if (isSystemAdmin && request.Dto.OriginalDoctorId.HasValue)
        {
            delegatorId = request.Dto.OriginalDoctorId.Value;
            if (!string.IsNullOrWhiteSpace(request.Dto.DelegatingDoctorName))
                delegatorName = request.Dto.DelegatingDoctorName;
        }
        else if (!string.IsNullOrWhiteSpace(request.Dto.DelegatingDoctorName))
        {
            var searchName = request.Dto.DelegatingDoctorName.Replace("Dr.", "").Replace("Dr ", "").Trim().ToLower();
            var delUser = await _context.Users
                .FirstOrDefaultAsync(u => 
                    (u.FirstName + " " + u.LastName).ToLower().Contains(searchName) ||
                    u.Username.ToLower().Contains(searchName), 
                    cancellationToken);
            if (delUser != null)
            {
                delegatorId = delUser.Id;
                delegatorName = string.IsNullOrWhiteSpace(request.Dto.DelegatingDoctorName)
                    ? $"{delUser.FirstName} {delUser.LastName}".Trim()
                    : request.Dto.DelegatingDoctorName;
            }
            else
            {
                delegatorName = request.Dto.DelegatingDoctorName;
            }
        }

        // Resolve Covering Doctor ID (Foreign Key requirement to Users table)
        Guid coveringId = Guid.Empty;
        if (request.Dto.CoveringDoctorId.HasValue && request.Dto.CoveringDoctorId.Value != Guid.Empty)
        {
            var exists = await _context.Users.AnyAsync(u => u.Id == request.Dto.CoveringDoctorId.Value, cancellationToken);
            if (exists)
                coveringId = request.Dto.CoveringDoctorId.Value;
        }

        if (coveringId == Guid.Empty && !string.IsNullOrWhiteSpace(request.Dto.CoveringDoctorName))
        {
            var searchName = request.Dto.CoveringDoctorName.Replace("Dr.", "").Replace("Dr ", "").Trim().ToLower();
            var targetUser = await _context.Users
                .FirstOrDefaultAsync(u => 
                    (u.FirstName + " " + u.LastName).ToLower().Contains(searchName) ||
                    u.Username.ToLower().Contains(searchName), 
                    cancellationToken);
            if (targetUser != null)
            {
                coveringId = targetUser.Id;
            }
        }

        // If covering doctor still not found by name, assign to first available doctor or delegator
        if (coveringId == Guid.Empty)
        {
            var fallbackDoctor = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id != delegatorId && u.UserRoles.Any(r => r.Role.Name == "Doctor"), cancellationToken);
            coveringId = fallbackDoctor?.Id ?? delegatorId;
        }

        var delegation = new DoctorDelegation
        {
            Id = Guid.NewGuid(),
            OriginalDoctorId = delegatorId,
            DelegatingDoctorName = delegatorName,
            CoveringDoctorId = coveringId,
            CoveringDoctorName = request.Dto.CoveringDoctorName,
            StartDateUtc = request.Dto.StartDateUtc,
            EndDateUtc = request.Dto.EndDateUtc,
            Reason = request.Dto.Reason ?? "Clinical Coverage / On-Call Rotation",
            Notes = request.Dto.Notes,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.DoctorDelegations.Add(delegation);
        await _context.SaveChangesAsync(cancellationToken);

        return new DoctorDelegationDto
        {
            Id = delegation.Id,
            OriginalDoctorId = delegation.OriginalDoctorId,
            DelegatingDoctorName = delegation.DelegatingDoctorName,
            CoveringDoctorId = delegation.CoveringDoctorId,
            CoveringDoctorName = delegation.CoveringDoctorName,
            StartDateUtc = delegation.StartDateUtc,
            EndDateUtc = delegation.EndDateUtc,
            Reason = delegation.Reason,
            Notes = delegation.Notes,
            IsActive = delegation.IsActive,
            CreatedAtUtc = delegation.CreatedAtUtc
        };
    }
}
