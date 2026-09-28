using System;
using System.Threading;
using System.Threading.Tasks;
using ClinicManagementSystem.Application.Common;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.Interfaces;
using ClinicManagementSystem.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Application.Inquiries.Commands;

public record CreateVisitorInquiryCommand(
    string VisitorName,
    string PhoneNumber,
    string InquiryType,
    string DepartmentTarget,
    string Notes,
    Guid HandledByUserId
) : IRequest<Result<VisitorInquiryResponseDto>>;

public class CreateVisitorInquiryCommandHandler : IRequestHandler<CreateVisitorInquiryCommand, Result<VisitorInquiryResponseDto>>
{
    private readonly IClinicDbContext _context;

    public CreateVisitorInquiryCommandHandler(IClinicDbContext context)
    {
        _context = context;
    }

    public async Task<Result<VisitorInquiryResponseDto>> Handle(CreateVisitorInquiryCommand request, CancellationToken cancellationToken)
    {
        var entity = new VisitorInquiry
        {
            Id = Guid.NewGuid(),
            VisitorName = request.VisitorName,
            PhoneNumber = request.PhoneNumber,
            InquiryType = string.IsNullOrWhiteSpace(request.InquiryType) ? "General Information" : request.InquiryType,
            DepartmentTarget = string.IsNullOrWhiteSpace(request.DepartmentTarget) ? "Reception" : request.DepartmentTarget,
            Notes = request.Notes,
            Status = "Open",
            HandledByUserId = request.HandledByUserId,
            CreatedAt = DateTime.UtcNow
        };

        _context.VisitorInquiries.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.HandledByUserId, cancellationToken);
        var userName = user != null ? $"{user.FirstName} {user.LastName}" : "Reception Staff";

        var responseDto = new VisitorInquiryResponseDto
        {
            Id = entity.Id,
            VisitorName = entity.VisitorName,
            PhoneNumber = entity.PhoneNumber,
            InquiryType = entity.InquiryType,
            DepartmentTarget = entity.DepartmentTarget,
            Notes = entity.Notes,
            Status = entity.Status,
            HandledByUserId = entity.HandledByUserId,
            HandledByUserName = userName,
            CreatedAt = entity.CreatedAt,
            ResolvedAt = entity.ResolvedAt
        };

        return Result<VisitorInquiryResponseDto>.Success(responseDto);
    }
}
