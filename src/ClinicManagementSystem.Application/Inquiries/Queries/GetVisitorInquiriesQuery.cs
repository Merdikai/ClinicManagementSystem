using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Application.Inquiries.Queries;

public record GetVisitorInquiriesQuery(string? Status = null) : IRequest<List<VisitorInquiryResponseDto>>;

public class GetVisitorInquiriesQueryHandler : IRequestHandler<GetVisitorInquiriesQuery, List<VisitorInquiryResponseDto>>
{
    private readonly IClinicDbContext _context;

    public GetVisitorInquiriesQueryHandler(IClinicDbContext context)
    {
        _context = context;
    }

    public async Task<List<VisitorInquiryResponseDto>> Handle(GetVisitorInquiriesQuery request, CancellationToken cancellationToken)
    {
        var query = _context.VisitorInquiries
            .Include(i => i.HandledByUser)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            query = query.Where(i => i.Status == request.Status);
        }

        var list = await query
            .OrderByDescending(i => i.CreatedAt)
            .Take(50)
            .ToListAsync(cancellationToken);

        return list.Select(i => new VisitorInquiryResponseDto
        {
            Id = i.Id,
            VisitorName = i.VisitorName,
            PhoneNumber = i.PhoneNumber,
            InquiryType = i.InquiryType,
            DepartmentTarget = i.DepartmentTarget,
            Notes = i.Notes,
            Status = i.Status,
            HandledByUserId = i.HandledByUserId,
            HandledByUserName = i.HandledByUser != null ? $"{i.HandledByUser.FirstName} {i.HandledByUser.LastName}" : "Reception Desk",
            CreatedAt = i.CreatedAt,
            ResolvedAt = i.ResolvedAt
        }).ToList();
    }
}
