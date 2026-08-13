using AutoMapper;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.Exceptions;
using ClinicManagementSystem.Domain.Entities;
using ClinicManagementSystem.Domain.Enums;
using ClinicManagementSystem.Domain.Interfaces;
using MediatR;

namespace ClinicManagementSystem.Application.Appointments.Commands;

public class CreateAppointmentCommandHandler : IRequestHandler<CreateAppointmentCommand, AppointmentResponseDto>
{
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IMapper _mapper;

    public CreateAppointmentCommandHandler(IAppointmentRepository appointmentRepository, IMapper mapper)
    {
        _appointmentRepository = appointmentRepository;
        _mapper = mapper;
    }

    public async Task<AppointmentResponseDto> Handle(CreateAppointmentCommand request, CancellationToken cancellationToken)
    {
        var isAvailable = await _appointmentRepository.IsSlotAvailableAsync(
            request.DoctorId, request.ScheduledDateTime, request.DurationMinutes);

        if (!isAvailable)
            throw new BusinessRuleViolationException("The requested time slot is not available.", "slot_unavailable");

        var dto = new CreateAppointmentDto(request.PatientId, request.DoctorId, request.ScheduledDateTime, request.DurationMinutes, request.ReasonForVisit);
        var appointment = _mapper.Map<Appointment>(dto);
        appointment.Status = AppointmentStatus.Scheduled;

        await _appointmentRepository.AddAsync(appointment);
        await _appointmentRepository.SaveChangesAsync();
        return _mapper.Map<AppointmentResponseDto>(appointment);
    }
}
