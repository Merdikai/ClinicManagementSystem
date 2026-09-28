using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.EmrAccess.DTOs;
using ClinicManagementSystem.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Application.EmrAccess.Queries;

public record GetEmrAccessAuditLogsQuery(int Page = 1, int PageSize = 20, Guid? PatientId = null, bool? EmergencyOnly = null) 
    : IRequest<PagedResponse<EmrAccessAuditLogDto>>;

public class GetEmrAccessAuditLogsQueryHandler : IRequestHandler<GetEmrAccessAuditLogsQuery, PagedResponse<EmrAccessAuditLogDto>>
{
    private readonly IClinicDbContext _context;

    public GetEmrAccessAuditLogsQueryHandler(IClinicDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResponse<EmrAccessAuditLogDto>> Handle(GetEmrAccessAuditLogsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.EmrAccessAuditLogs
            .Include(l => l.Patient)
            .AsQueryable();

        if (request.PatientId.HasValue)
            query = query.Where(l => l.PatientId == request.PatientId.Value);

        if (request.EmergencyOnly.HasValue && request.EmergencyOnly.Value)
            query = query.Where(l => l.IsBreakGlassEmergency);

        var totalCount = await query.CountAsync(cancellationToken);
        var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);
        var hasNext = request.Page < totalPages;
        var hasPrev = request.Page > 1;

        var items = await query
            .OrderByDescending(l => l.Timestamp)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(l => new EmrAccessAuditLogDto
            {
                Id = l.Id,
                PatientId = l.PatientId,
                PatientName = l.Patient != null ? (l.Patient.FirstName + " " + l.Patient.LastName) : "Patient Record",
                AccessedByUserId = l.UserId.ToString(),
                AccessedByUserName = l.UserName,
                AccessedByRole = l.UserRole,
                AccessType = l.AccessType,
                ActionDetail = l.Reason,
                IsEmergencyOverride = l.IsBreakGlassEmergency,
                BreakGlassRationale = l.Reason,
                AccessedAtUtc = l.Timestamp
            })
            .ToListAsync(cancellationToken);

        return new PagedResponse<EmrAccessAuditLogDto>(items, totalCount, request.Page, request.PageSize, totalPages, hasNext, hasPrev);
    }
}
