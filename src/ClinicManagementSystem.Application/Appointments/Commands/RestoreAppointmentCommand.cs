using AutoMapper;
using ClinicManagementSystem.Application.Common;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace ClinicManagementSystem.Application.Appointments.Commands;

public record RestoreAppointmentCommand(Guid Id) : IRequest<Result<bool>>;

public class RestoreAppointmentCommandHandler : IRequestHandler<RestoreAppointmentCommand, Result<bool>>
{
    private readonly IClinicDbContext _context;
    private readonly HybridCache _cache;

    public RestoreAppointmentCommandHandler(IClinicDbContext context, HybridCache cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<Result<bool>> Handle(RestoreAppointmentCommand request, CancellationToken cancellationToken)
    {
        var appointment = await _context.Appointments
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(a => a.Id == request.Id && a.IsDeleted, cancellationToken);

        if (appointment is null)
            return Result<bool>.Failure($"Deleted appointment '{request.Id}' was not found.", "NOT_FOUND");

        appointment.IsDeleted = false;
        appointment.DeletedAt = null;
        if (appointment.Status == Domain.Enums.AppointmentStatus.Cancelled)
            appointment.Status = Domain.Enums.AppointmentStatus.Scheduled;

        await _context.SaveChangesAsync(cancellationToken);

        try
        {
            await _cache.RemoveByTagAsync("appointments", cancellationToken);
        }
        catch { }

        return Result<bool>.Success(true);
    }
}

public record GetDeletedAppointmentsQuery : IRequest<List<AppointmentResponseDto>>;

public class GetDeletedAppointmentsQueryHandler : IRequestHandler<GetDeletedAppointmentsQuery, List<AppointmentResponseDto>>
{
    private readonly IClinicDbContext _context;
    private readonly IMapper _mapper;

    public GetDeletedAppointmentsQueryHandler(IClinicDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<List<AppointmentResponseDto>> Handle(GetDeletedAppointmentsQuery request, CancellationToken cancellationToken)
    {
        var deleted = await _context.Appointments
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .Where(a => a.IsDeleted)
            .OrderByDescending(a => a.DeletedAt)
            .ToListAsync(cancellationToken);

        return _mapper.Map<List<AppointmentResponseDto>>(deleted);
    }
}

