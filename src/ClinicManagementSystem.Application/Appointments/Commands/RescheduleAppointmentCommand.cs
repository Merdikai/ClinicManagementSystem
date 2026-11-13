using AutoMapper;
using ClinicManagementSystem.Application.Common;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.Interfaces;
using ClinicManagementSystem.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace ClinicManagementSystem.Application.Appointments.Commands;

public record RescheduleAppointmentCommand(
    Guid Id,
    DateTime NewScheduledDateTime,
    string? Reason = null
) : IRequest<Result<AppointmentResponseDto>>;

public class RescheduleAppointmentCommandHandler : IRequestHandler<RescheduleAppointmentCommand, Result<AppointmentResponseDto>>
{
    private readonly IClinicDbContext _context;
    private readonly IMapper _mapper;
    private readonly HybridCache? _cache;

    public RescheduleAppointmentCommandHandler(IClinicDbContext context, IMapper mapper, HybridCache? cache = null)
    {
        _context = context;
        _mapper = mapper;
        _cache = cache;
    }

    public async Task<Result<AppointmentResponseDto>> Handle(RescheduleAppointmentCommand request, CancellationToken cancellationToken)
    {
        var appointment = await _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .Include(a => a.VitalSigns)
            .FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);

        if (appointment == null)
        {
            return Result<AppointmentResponseDto>.Failure($"Appointment with ID '{request.Id}' was not found.", "NOT_FOUND");
        }

        if (appointment.Status == AppointmentStatus.Completed)
        {
            return Result<AppointmentResponseDto>.Failure("Completed appointments cannot be rescheduled.", "INVALID_OPERATION");
        }

        appointment.ScheduledDateTime = request.NewScheduledDateTime;
        appointment.Status = AppointmentStatus.Scheduled;

        if (!string.IsNullOrWhiteSpace(request.Reason))
        {
            appointment.ReasonForVisit = $"{appointment.ReasonForVisit} (Rescheduled: {request.Reason})";
        }

        await _context.SaveChangesAsync(cancellationToken);

        if (_cache is not null)
        {
            try
            {
                await _cache.RemoveByTagAsync("appointments", cancellationToken);
                await _cache.RemoveAsync($"appointment_v1:{request.Id}", cancellationToken);
            }
            catch { }
        }

        var dto = _mapper.Map<AppointmentResponseDto>(appointment);
        return Result<AppointmentResponseDto>.Success(dto);
    }
}
