using AutoMapper;
using ClinicManagementSystem.Application.Common;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.Interfaces;
using ClinicManagementSystem.Domain.Entities;
using ClinicManagementSystem.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

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

    public RescheduleAppointmentCommandHandler(IClinicDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
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

        var dto = _mapper.Map<AppointmentResponseDto>(appointment);
        return Result<AppointmentResponseDto>.Success(dto);
    }
}
