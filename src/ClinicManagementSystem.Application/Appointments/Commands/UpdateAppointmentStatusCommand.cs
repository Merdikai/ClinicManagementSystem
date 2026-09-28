using ClinicManagementSystem.Application.Common;
using ClinicManagementSystem.Application.DTOs;
using MediatR;

namespace ClinicManagementSystem.Application.Appointments.Commands;

public record UpdateAppointmentStatusCommand(
    Guid AppointmentId,
    string Status,
    string? RoomAssignment = null
) : IRequest<Result<AppointmentResponseDto>>;
