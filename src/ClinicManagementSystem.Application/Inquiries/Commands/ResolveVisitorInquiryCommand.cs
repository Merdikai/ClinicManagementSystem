using System;
using System.Threading;
using System.Threading.Tasks;
using ClinicManagementSystem.Application.Common;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Application.Inquiries.Commands;

public record ResolveVisitorInquiryCommand(Guid Id, string? Notes = null) : IRequest<Result<VisitorInquiryResponseDto>>;

public class ResolveVisitorInquiryCommandHandler : IRequestHandler<ResolveVisitorInquiryCommand, Result<VisitorInquiryResponseDto>>
{
    private readonly IClinicDbContext _context;

    public ResolveVisitorInquiryCommandHandler(IClinicDbContext context)
    {
        _context = context;
    }

    public async Task<Result<VisitorInquiryResponseDto>> Handle(ResolveVisitorInquiryCommand request, CancellationToken cancellationToken)
    {
        var inquiry = await _context.VisitorInquiries
            .Include(i => i.HandledByUser)
            .FirstOrDefaultAsync(i => i.Id == request.Id, cancellationToken);

        if (inquiry == null)
        {
            return Result<VisitorInquiryResponseDto>.Failure($"Visitor inquiry with ID '{request.Id}' was not found.", "NOT_FOUND");
        }

        inquiry.Status = "Resolved";
        inquiry.ResolvedAt = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(request.Notes))
        {
            inquiry.Notes = $"{inquiry.Notes} [Resolution: {request.Notes}]";
        }

        await _context.SaveChangesAsync(cancellationToken);

        var responseDto = new VisitorInquiryResponseDto
        {
            Id = inquiry.Id,
            VisitorName = inquiry.VisitorName,
            PhoneNumber = inquiry.PhoneNumber,
            InquiryType = inquiry.InquiryType,
            DepartmentTarget = inquiry.DepartmentTarget,
            Notes = inquiry.Notes,
            Status = inquiry.Status,
            HandledByUserId = inquiry.HandledByUserId,
            HandledByUserName = inquiry.HandledByUser != null ? $"{inquiry.HandledByUser.FirstName} {inquiry.HandledByUser.LastName}" : "Reception Staff",
            CreatedAt = inquiry.CreatedAt,
            ResolvedAt = inquiry.ResolvedAt
        };

        return Result<VisitorInquiryResponseDto>.Success(responseDto);
    }
}
