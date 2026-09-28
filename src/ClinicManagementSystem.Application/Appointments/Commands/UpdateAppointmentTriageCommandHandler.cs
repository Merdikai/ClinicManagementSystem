using AutoMapper;
using ClinicManagementSystem.Application.Common;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.Exceptions;
using ClinicManagementSystem.Domain.Entities;
using ClinicManagementSystem.Domain.Enums;
using ClinicManagementSystem.Domain.Interfaces;
using MediatR;

namespace ClinicManagementSystem.Application.Appointments.Commands;

public class UpdateAppointmentTriageCommandHandler : IRequestHandler<UpdateAppointmentTriageCommand, Result<AppointmentResponseDto>>
{
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IMapper _mapper;

    public UpdateAppointmentTriageCommandHandler(IAppointmentRepository appointmentRepository, IMapper mapper)
    {
        _appointmentRepository = appointmentRepository;
        _mapper = mapper;
    }

    public async Task<Result<AppointmentResponseDto>> Handle(UpdateAppointmentTriageCommand request, CancellationToken cancellationToken)
    {
        var appointment = await _appointmentRepository.GetByIdAsync(request.AppointmentId);
        if (appointment is null)
            return Result<AppointmentResponseDto>.Failure($"Appointment {request.AppointmentId} not found", "appointment_not_found");

        if (!string.IsNullOrWhiteSpace(request.TriageUrgency))
            appointment.TriageUrgency = request.TriageUrgency;

        if (!string.IsNullOrWhiteSpace(request.RoomAssignment))
            appointment.RoomAssignment = request.RoomAssignment;

        if (!string.IsNullOrWhiteSpace(request.TriageNotes))
            appointment.TriageNotes = request.TriageNotes;

        if (!string.IsNullOrWhiteSpace(request.Status) && Enum.TryParse<AppointmentStatus>(request.Status, true, out var parsedStatus))
        {
            appointment.Status = parsedStatus;
        }
        else if (appointment.Status == AppointmentStatus.CheckedIn)
        {
            appointment.Status = AppointmentStatus.TriageComplete;
        }

        _appointmentRepository.Update(appointment);
        await _appointmentRepository.SaveChangesAsync();

        return Result<AppointmentResponseDto>.Success(_mapper.Map<AppointmentResponseDto>(appointment));
    }
}
