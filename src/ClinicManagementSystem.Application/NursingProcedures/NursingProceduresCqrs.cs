using AutoMapper;
using ClinicManagementSystem.Application.Common;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.Interfaces;
using ClinicManagementSystem.Domain.Entities;
using ClinicManagementSystem.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Application.NursingProcedures;

public record GetNursingProceduresQuery(
    Guid? PatientId = null,
    Guid? AppointmentId = null,
    DateTime? Date = null,
    string? Status = null) : IRequest<IEnumerable<NursingProcedureResponseDto>>;

public record GetNursingProcedureByIdQuery(Guid Id) : IRequest<NursingProcedureResponseDto?>;

public record CreateNursingProcedureCommand(
    Guid PatientId,
    Guid? AppointmentId,
    Guid PerformerUserId,
    string ProcedureType,
    string? BodySite,
    string? Notes,
    string? PatientTolerance,
    string? SuppliesUsed,
    DateTime? PerformedAt) : IRequest<Result<NursingProcedureResponseDto>>;

public record OrderNursingProcedureCommand(
    Guid PatientId,
    Guid? AppointmentId,
    Guid? OrderedByDoctorId,
    string ProcedureType,
    string? BodySite,
    string? Notes,
    string? Priority,
    decimal Price) : IRequest<Result<NursingProcedureResponseDto>>;

public record CompleteNursingProcedureCommand(
    Guid Id,
    Guid NurseUserId,
    string? PatientTolerance,
    string? SuppliesUsed,
    string? Notes) : IRequest<Result<NursingProcedureResponseDto>>;

public class CreateNursingProcedureCommandValidator : AbstractValidator<CreateNursingProcedureCommand>
{
    public CreateNursingProcedureCommandValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.PerformerUserId).NotEmpty();
        RuleFor(x => x.ProcedureType).NotEmpty().MaximumLength(150);
    }
}

public class OrderNursingProcedureCommandValidator : AbstractValidator<OrderNursingProcedureCommand>
{
    public OrderNursingProcedureCommandValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.ProcedureType).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0m);
    }
}

public class CompleteNursingProcedureCommandValidator : AbstractValidator<CompleteNursingProcedureCommand>
{
    public CompleteNursingProcedureCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.NurseUserId).NotEmpty();
    }
}

public class GetNursingProceduresQueryHandler : IRequestHandler<GetNursingProceduresQuery, IEnumerable<NursingProcedureResponseDto>>
{
    private readonly IClinicDbContext _context;
    private readonly IMapper _mapper;

    public GetNursingProceduresQueryHandler(IClinicDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<IEnumerable<NursingProcedureResponseDto>> Handle(GetNursingProceduresQuery request, CancellationToken cancellationToken)
    {
        var query = _context.NursingProcedures
            .AsNoTracking()
            .Include(p => p.Patient)
            .Include(p => p.PerformedByUser)
            .Include(p => p.OrderedByDoctor)
            .AsQueryable();

        if (request.PatientId.HasValue && request.PatientId.Value != Guid.Empty)
            query = query.Where(p => p.PatientId == request.PatientId.Value);

        if (request.AppointmentId.HasValue && request.AppointmentId.Value != Guid.Empty)
            query = query.Where(p => p.AppointmentId == request.AppointmentId.Value);

        if (!string.IsNullOrWhiteSpace(request.Status))
            query = query.Where(p => p.Status == request.Status);

        if (request.Date.HasValue)
        {
            var start = request.Date.Value.Date;
            var end = start.AddDays(1);
            query = query.Where(p =>
                (p.PerformedAt.HasValue && p.PerformedAt.Value >= start && p.PerformedAt.Value < end) ||
                (p.OrderedAt >= start && p.OrderedAt < end));
        }

        var items = await query.OrderByDescending(p => p.OrderedAt).ToListAsync(cancellationToken);
        return _mapper.Map<IEnumerable<NursingProcedureResponseDto>>(items);
    }
}

public class GetNursingProcedureByIdQueryHandler : IRequestHandler<GetNursingProcedureByIdQuery, NursingProcedureResponseDto?>
{
    private readonly IClinicDbContext _context;
    private readonly IMapper _mapper;

    public GetNursingProcedureByIdQueryHandler(IClinicDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<NursingProcedureResponseDto?> Handle(GetNursingProcedureByIdQuery request, CancellationToken cancellationToken)
    {
        var item = await _context.NursingProcedures
            .AsNoTracking()
            .Include(p => p.Patient)
            .Include(p => p.PerformedByUser)
            .Include(p => p.OrderedByDoctor)
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        return item is null ? null : _mapper.Map<NursingProcedureResponseDto>(item);
    }
}

public class CreateNursingProcedureCommandHandler : IRequestHandler<CreateNursingProcedureCommand, Result<NursingProcedureResponseDto>>
{
    private readonly IClinicDbContext _context;
    private readonly IMapper _mapper;

    public CreateNursingProcedureCommandHandler(IClinicDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<Result<NursingProcedureResponseDto>> Handle(CreateNursingProcedureCommand request, CancellationToken cancellationToken)
    {
        var procedure = new NursingProcedure
        {
            Id = Guid.NewGuid(),
            PatientId = request.PatientId,
            AppointmentId = request.AppointmentId,
            PerformedByUserId = request.PerformerUserId,
            ProcedureType = request.ProcedureType,
            BodySite = request.BodySite ?? string.Empty,
            Notes = request.Notes ?? string.Empty,
            PatientTolerance = request.PatientTolerance ?? "Well Tolerated",
            SuppliesUsed = request.SuppliesUsed ?? string.Empty,
            Status = "Completed",
            Priority = "Routine",
            OrderedAt = DateTime.UtcNow,
            PerformedAt = request.PerformedAt ?? DateTime.UtcNow,
            Price = 30.00m
        };

        _context.NursingProcedures.Add(procedure);

        if (request.AppointmentId.HasValue && request.AppointmentId.Value != Guid.Empty)
        {
            var appointment = await _context.Appointments.FirstOrDefaultAsync(a => a.Id == request.AppointmentId.Value, cancellationToken);
            if (appointment != null && (appointment.Status == AppointmentStatus.CheckedIn || appointment.Status == AppointmentStatus.TriageComplete))
            {
                appointment.Status = AppointmentStatus.InProcedure;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        var created = await _context.NursingProcedures
            .AsNoTracking()
            .Include(p => p.Patient)
            .Include(p => p.PerformedByUser)
            .Include(p => p.OrderedByDoctor)
            .FirstAsync(p => p.Id == procedure.Id, cancellationToken);

        return Result<NursingProcedureResponseDto>.Success(_mapper.Map<NursingProcedureResponseDto>(created));
    }
}

public class OrderNursingProcedureCommandHandler : IRequestHandler<OrderNursingProcedureCommand, Result<NursingProcedureResponseDto>>
{
    private readonly IClinicDbContext _context;
    private readonly IMapper _mapper;

    public OrderNursingProcedureCommandHandler(IClinicDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<Result<NursingProcedureResponseDto>> Handle(OrderNursingProcedureCommand request, CancellationToken cancellationToken)
    {
        var procedure = new NursingProcedure
        {
            Id = Guid.NewGuid(),
            PatientId = request.PatientId,
            AppointmentId = request.AppointmentId,
            OrderedByDoctorId = request.OrderedByDoctorId != Guid.Empty ? request.OrderedByDoctorId : null,
            ProcedureType = request.ProcedureType,
            BodySite = request.BodySite ?? string.Empty,
            Notes = request.Notes ?? string.Empty,
            PatientTolerance = "Pending Procedure",
            SuppliesUsed = string.Empty,
            Status = "Ordered",
            Priority = string.IsNullOrWhiteSpace(request.Priority) ? "Routine" : request.Priority,
            Price = request.Price > 0m ? request.Price : 30.00m,
            OrderedAt = DateTime.UtcNow
        };

        _context.NursingProcedures.Add(procedure);
        await _context.SaveChangesAsync(cancellationToken);

        var created = await _context.NursingProcedures
            .AsNoTracking()
            .Include(p => p.Patient)
            .Include(p => p.PerformedByUser)
            .Include(p => p.OrderedByDoctor)
            .FirstAsync(p => p.Id == procedure.Id, cancellationToken);

        return Result<NursingProcedureResponseDto>.Success(_mapper.Map<NursingProcedureResponseDto>(created));
    }
}

public class CompleteNursingProcedureCommandHandler : IRequestHandler<CompleteNursingProcedureCommand, Result<NursingProcedureResponseDto>>
{
    private readonly IClinicDbContext _context;
    private readonly IMapper _mapper;

    public CompleteNursingProcedureCommandHandler(IClinicDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<Result<NursingProcedureResponseDto>> Handle(CompleteNursingProcedureCommand request, CancellationToken cancellationToken)
    {
        var procedure = await _context.NursingProcedures
            .Include(p => p.Patient)
            .Include(p => p.OrderedByDoctor)
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (procedure is null)
            return Result<NursingProcedureResponseDto>.Failure($"Nursing procedure {request.Id} not found.", "NOT_FOUND");

        procedure.PerformedByUserId = request.NurseUserId;
        procedure.PerformedAt = DateTime.UtcNow;
        procedure.Status = "Completed";
        if (!string.IsNullOrWhiteSpace(request.PatientTolerance))
            procedure.PatientTolerance = request.PatientTolerance;
        if (!string.IsNullOrWhiteSpace(request.SuppliesUsed))
            procedure.SuppliesUsed = request.SuppliesUsed;
        if (!string.IsNullOrWhiteSpace(request.Notes))
        {
            procedure.Notes = string.IsNullOrWhiteSpace(procedure.Notes)
                ? request.Notes
                : $"{procedure.Notes} | Nurse completion note: {request.Notes}";
        }

        await _context.SaveChangesAsync(cancellationToken);

        var completed = await _context.NursingProcedures
            .AsNoTracking()
            .Include(p => p.Patient)
            .Include(p => p.PerformedByUser)
            .Include(p => p.OrderedByDoctor)
            .FirstAsync(p => p.Id == procedure.Id, cancellationToken);

        return Result<NursingProcedureResponseDto>.Success(_mapper.Map<NursingProcedureResponseDto>(completed));
    }
}

