using AutoMapper;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.Exceptions;
using ClinicManagementSystem.Domain.Entities;
using ClinicManagementSystem.Domain.Enums;
using ClinicManagementSystem.Domain.Interfaces;
using MediatR;

namespace ClinicManagementSystem.Application.Appointments.Commands;

using ClinicManagementSystem.Application.Common;
using ClinicManagementSystem.Application.Interfaces;
using Microsoft.Extensions.Caching.Hybrid;

public class CreateAppointmentCommandHandler : IRequestHandler<CreateAppointmentCommand, Result<AppointmentResponseDto>>
{
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IMapper _mapper;
    private readonly ILinkGeneratorService _linkGenerator;
    private readonly INotificationService _notificationService;
    private readonly HybridCache? _cache;

    public CreateAppointmentCommandHandler(
        IAppointmentRepository appointmentRepository,
        IMapper mapper,
        ILinkGeneratorService linkGenerator,
        INotificationService notificationService,
        HybridCache? cache = null)
    {
        _appointmentRepository = appointmentRepository;
        _mapper = mapper;
        _linkGenerator = linkGenerator;
        _notificationService = notificationService;
        _cache = cache;
    }

    public async Task<Result<AppointmentResponseDto>> Handle(CreateAppointmentCommand request, CancellationToken cancellationToken)
    {
        var isAvailable = await _appointmentRepository.IsSlotAvailableAsync(
            request.DoctorId, request.ScheduledDateTime, request.DurationMinutes, cancellationToken);

        if (!isAvailable)
            return Result<AppointmentResponseDto>.Failure("The requested time slot is not available.", "slot_unavailable");

        var dto = new CreateAppointmentDto(request.PatientId, request.DoctorId, request.ScheduledDateTime, request.DurationMinutes, request.ReasonForVisit);
        var appointment = _mapper.Map<Appointment>(dto);
        appointment.Status = AppointmentStatus.Scheduled;

        await _appointmentRepository.AddAsync(appointment, cancellationToken);
        await _appointmentRepository.SaveChangesAsync(cancellationToken);

        if (_cache is not null)
        {
            try { await _cache.RemoveByTagAsync("appointments", cancellationToken); } catch { }
        }

        // Notify doctor about new appointment
        await _notificationService.NotifyAppointmentBookedAsync(
            request.DoctorId,
            appointment.Id,
            appointment.Patient != null ? $"{appointment.Patient.FirstName} {appointment.Patient.LastName}" : "Unknown Patient"
        );

        var responseDto = _mapper.Map<AppointmentResponseDto>(appointment);
        responseDto.Links = _linkGenerator.GenerateAppointmentLinks(appointment);
        return Result<AppointmentResponseDto>.Success(responseDto);
    }
}
