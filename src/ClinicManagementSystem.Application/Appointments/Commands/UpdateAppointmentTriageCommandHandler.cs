using AutoMapper;
using ClinicManagementSystem.Application.Common;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Domain.Enums;
using ClinicManagementSystem.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;

namespace ClinicManagementSystem.Application.Appointments.Commands;

public class UpdateAppointmentTriageCommandHandler : IRequestHandler<UpdateAppointmentTriageCommand, Result<AppointmentResponseDto>>
{
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IMapper _mapper;
    private readonly HybridCache? _cache;

    public UpdateAppointmentTriageCommandHandler(
        IAppointmentRepository appointmentRepository,
        IMapper mapper,
        HybridCache? cache = null)
    {
        _appointmentRepository = appointmentRepository;
        _mapper = mapper;
        _cache = cache;
    }

    public async Task<Result<AppointmentResponseDto>> Handle(UpdateAppointmentTriageCommand request, CancellationToken cancellationToken)
    {
        var appointment = await _appointmentRepository.GetByIdAsync(request.AppointmentId, cancellationToken);
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
        await _appointmentRepository.SaveChangesAsync(cancellationToken);

        if (_cache is not null)
        {
            try
            {
                await _cache.RemoveByTagAsync("appointments", cancellationToken);
                await _cache.RemoveAsync($"appointment_v1:{request.AppointmentId}", cancellationToken);
            }
            catch { }
        }

        return Result<AppointmentResponseDto>.Success(_mapper.Map<AppointmentResponseDto>(appointment));
    }
}
