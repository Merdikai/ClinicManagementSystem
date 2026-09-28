using AutoMapper;
using ClinicManagementSystem.Application.Common;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.Exceptions;
using ClinicManagementSystem.Application.Interfaces;
using ClinicManagementSystem.Domain.Entities;
using ClinicManagementSystem.Domain.Enums;
using ClinicManagementSystem.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Application.Appointments.Commands;

public class CheckInAppointmentCommandHandler : IRequestHandler<CheckInAppointmentCommand, Result<AppointmentResponseDto>>
{
    private readonly IClinicDbContext _context;
    private readonly IMapper _mapper;
    private readonly INotificationService _notificationService;

    public CheckInAppointmentCommandHandler(
        IClinicDbContext context,
        IMapper mapper,
        INotificationService notificationService)
    {
        _context = context;
        _mapper = mapper;
        _notificationService = notificationService;
    }

    public async Task<Result<AppointmentResponseDto>> Handle(CheckInAppointmentCommand request, CancellationToken cancellationToken)
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

        if (appointment.Status != AppointmentStatus.Scheduled && appointment.Status != AppointmentStatus.PendingApproval)
        {
            return Result<AppointmentResponseDto>.Failure($"Only scheduled or pending appointments can be checked in. Current status is {appointment.Status}.", "INVALID_STATUS");
        }

        // Assign or generate token
        if (!string.IsNullOrWhiteSpace(request.TokenNumber))
        {
            appointment.TokenNumber = request.TokenNumber.Trim();
        }
        else if (string.IsNullOrWhiteSpace(appointment.TokenNumber))
        {
            var today = DateTime.UtcNow.Date;
            var todayCount = await _context.Appointments
                .CountAsync(a => a.ScheduledDateTime.Date == today && a.TokenNumber != null, cancellationToken);
            appointment.TokenNumber = $"Q-{(todayCount + 1):D3}";
        }

        // Assign visit type
        if (!string.IsNullOrWhiteSpace(request.VisitType))
        {
            appointment.VisitType = request.VisitType.Trim();
        }
        else if (string.IsNullOrWhiteSpace(appointment.VisitType))
        {
            appointment.VisitType = "General Consultation";
        }

        appointment.Status = AppointmentStatus.CheckedIn;
        await _context.SaveChangesAsync(cancellationToken);

        // Notify doctor that patient is ready
        try
        {
            await _notificationService.NotifyPatientCheckedInAsync(
                appointment.DoctorId,
                appointment.Id,
                appointment.Patient != null ? $"{appointment.Patient.FirstName} {appointment.Patient.LastName}" : "Unknown Patient"
            );
        }
        catch { }

        var dto = _mapper.Map<AppointmentResponseDto>(appointment);
        return Result<AppointmentResponseDto>.Success(dto);
    }
}
