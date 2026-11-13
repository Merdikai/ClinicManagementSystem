using AutoMapper;
using ClinicManagementSystem.Application.Common;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.Interfaces;
using ClinicManagementSystem.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Application.ShiftHandovers;

public record GetShiftHandoversQuery(DateTime? Date = null, string? ShiftType = null) : IRequest<IEnumerable<ShiftHandoverResponseDto>>;
public record GetLatestShiftHandoverQuery : IRequest<ShiftHandoverResponseDto?>;
public record CreateShiftHandoverCommand(
    Guid NurseUserId,
    string ShiftType,
    DateTime? HandoverTimestamp,
    string Situation,
    string? Background,
    string? Assessment,
    string? Recommendation,
    int ActivePatientCount,
    int CriticalAlertsCount) : IRequest<Result<ShiftHandoverResponseDto>>;

public class CreateShiftHandoverCommandValidator : AbstractValidator<CreateShiftHandoverCommand>
{
    public CreateShiftHandoverCommandValidator()
    {
        RuleFor(x => x.NurseUserId).NotEmpty();
        RuleFor(x => x.ShiftType).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Situation).NotEmpty().MaximumLength(2000);
    }
}

public class GetShiftHandoversQueryHandler : IRequestHandler<GetShiftHandoversQuery, IEnumerable<ShiftHandoverResponseDto>>
{
    private readonly IClinicDbContext _context;
    private readonly IMapper _mapper;

    public GetShiftHandoversQueryHandler(IClinicDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<IEnumerable<ShiftHandoverResponseDto>> Handle(GetShiftHandoversQuery request, CancellationToken cancellationToken)
    {
        var query = _context.ShiftHandovers
            .AsNoTracking()
            .Include(s => s.NurseUser)
            .AsQueryable();

        if (request.Date.HasValue)
        {
            var start = request.Date.Value.Date;
            var end = start.AddDays(1);
            query = query.Where(s => s.HandoverTimestamp >= start && s.HandoverTimestamp < end);
        }

        if (!string.IsNullOrWhiteSpace(request.ShiftType))
            query = query.Where(s => s.ShiftType == request.ShiftType);

        var items = await query.OrderByDescending(s => s.HandoverTimestamp).ToListAsync(cancellationToken);
        return _mapper.Map<IEnumerable<ShiftHandoverResponseDto>>(items);
    }
}

public class GetLatestShiftHandoverQueryHandler : IRequestHandler<GetLatestShiftHandoverQuery, ShiftHandoverResponseDto?>
{
    private readonly IClinicDbContext _context;
    private readonly IMapper _mapper;

    public GetLatestShiftHandoverQueryHandler(IClinicDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<ShiftHandoverResponseDto?> Handle(GetLatestShiftHandoverQuery request, CancellationToken cancellationToken)
    {
        var item = await _context.ShiftHandovers
            .AsNoTracking()
            .Include(s => s.NurseUser)
            .OrderByDescending(s => s.HandoverTimestamp)
            .FirstOrDefaultAsync(cancellationToken);

        return item is null ? null : _mapper.Map<ShiftHandoverResponseDto>(item);
    }
}

public class CreateShiftHandoverCommandHandler : IRequestHandler<CreateShiftHandoverCommand, Result<ShiftHandoverResponseDto>>
{
    private readonly IClinicDbContext _context;
    private readonly IMapper _mapper;

    public CreateShiftHandoverCommandHandler(IClinicDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<Result<ShiftHandoverResponseDto>> Handle(CreateShiftHandoverCommand request, CancellationToken cancellationToken)
    {
        var handover = new ShiftHandover
        {
            Id = Guid.NewGuid(),
            NurseUserId = request.NurseUserId,
            ShiftType = request.ShiftType,
            HandoverTimestamp = request.HandoverTimestamp ?? DateTime.UtcNow,
            Situation = request.Situation,
            Background = request.Background ?? string.Empty,
            Assessment = request.Assessment ?? string.Empty,
            Recommendation = request.Recommendation ?? string.Empty,
            ActivePatientCount = request.ActivePatientCount,
            CriticalAlertsCount = request.CriticalAlertsCount
        };

        _context.ShiftHandovers.Add(handover);
        await _context.SaveChangesAsync(cancellationToken);

        var created = await _context.ShiftHandovers
            .AsNoTracking()
            .Include(s => s.NurseUser)
            .FirstAsync(s => s.Id == handover.Id, cancellationToken);

        return Result<ShiftHandoverResponseDto>.Success(_mapper.Map<ShiftHandoverResponseDto>(created));
    }
}

