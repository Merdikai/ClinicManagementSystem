using ClinicManagementSystem.Application.Common;
using ClinicManagementSystem.Application.DTOs;
using MediatR;

namespace ClinicManagementSystem.Application.Appointments.Commands;

public record CheckInAppointmentCommand(
    Guid Id,
    string? TokenNumber = null,
    string? VisitType = null
) : IRequest<Result<AppointmentResponseDto>>;
