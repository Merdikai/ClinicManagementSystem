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

public record GetActiveDoctorDelegationsQuery : IRequest<List<DoctorDelegationDto>>;

public class GetActiveDoctorDelegationsQueryHandler : IRequestHandler<GetActiveDoctorDelegationsQuery, List<DoctorDelegationDto>>
{
    private readonly IClinicDbContext _context;

    public GetActiveDoctorDelegationsQueryHandler(IClinicDbContext context)
    {
        _context = context;
    }

    public async Task<List<DoctorDelegationDto>> Handle(GetActiveDoctorDelegationsQuery request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        return await _context.DoctorDelegations
            .Where(d => d.IsActive && d.StartDateUtc <= now && d.EndDateUtc >= now)
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
