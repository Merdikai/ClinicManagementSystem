using AutoMapper;
using ClinicManagementSystem.Application.Common;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Domain.Enums;
using ClinicManagementSystem.Domain.Interfaces;
using MediatR;

namespace ClinicManagementSystem.Application.Appointments.Commands;

public class UpdateAppointmentStatusCommandHandler : IRequestHandler<UpdateAppointmentStatusCommand, Result<AppointmentResponseDto>>
{
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IMapper _mapper;

    public UpdateAppointmentStatusCommandHandler(IAppointmentRepository appointmentRepository, IMapper mapper)
    {
        _appointmentRepository = appointmentRepository;
        _mapper = mapper;
    }

    public async Task<Result<AppointmentResponseDto>> Handle(UpdateAppointmentStatusCommand request, CancellationToken cancellationToken)
    {
        var appointment = await _appointmentRepository.GetByIdAsync(request.AppointmentId);
        if (appointment is null)
            return Result<AppointmentResponseDto>.Failure($"Appointment {request.AppointmentId} not found", "appointment_not_found");

        if (!Enum.TryParse<AppointmentStatus>(request.Status, true, out var parsedStatus))
            return Result<AppointmentResponseDto>.Failure($"Invalid appointment status '{request.Status}'", "invalid_status");

        appointment.Status = parsedStatus;

        if (!string.IsNullOrWhiteSpace(request.RoomAssignment))
            appointment.RoomAssignment = request.RoomAssignment;

        _appointmentRepository.Update(appointment);
        await _appointmentRepository.SaveChangesAsync();

        return Result<AppointmentResponseDto>.Success(_mapper.Map<AppointmentResponseDto>(appointment));
    }
}
