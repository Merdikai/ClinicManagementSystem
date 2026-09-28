using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ClinicManagementSystem.Application.DoctorDelegations.DTOs;
using ClinicManagementSystem.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Application.DoctorDelegations.Queries;

public record GetMyDoctorDelegationsQuery : IRequest<List<DoctorDelegationDto>>;

public class GetMyDoctorDelegationsQueryHandler : IRequestHandler<GetMyDoctorDelegationsQuery, List<DoctorDelegationDto>>
{
    private readonly IClinicDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetMyDoctorDelegationsQueryHandler(IClinicDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<List<DoctorDelegationDto>> Handle(GetMyDoctorDelegationsQuery request, CancellationToken cancellationToken)
    {
        var userGuid = _currentUser.UserId;
        if (!userGuid.HasValue)
            return new List<DoctorDelegationDto>();

        return await _context.DoctorDelegations
            .Where(d => d.OriginalDoctorId == userGuid.Value || d.CoveringDoctorId == userGuid.Value)
            .OrderByDescending(d => d.CreatedAtUtc)
            .Select(d => new DoctorDelegationDto
            {
                Id = d.Id,
                OriginalDoctorId = d.OriginalDoctorId,
                DelegatingDoctorName = d.DelegatingDoctorName,
                CoveringDoctorId = d.CoveringDoctorId,
                CoveringDoctorName = d.CoveringDoctorName,
                StartDateUtc = d.StartDateUtc,
                EndDateUtc = d.EndDateUtc,
                Reason = d.Reason,
                Notes = d.Notes,
                IsActive = d.IsActive,
                CreatedAtUtc = d.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);
    }
}
