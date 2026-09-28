using ClinicManagementSystem.Application.Common;
using ClinicManagementSystem.Application.DTOs;
using MediatR;

namespace ClinicManagementSystem.Application.Appointments.Commands;

public record UpdateAppointmentTriageCommand(
    Guid AppointmentId,
    string? TriageUrgency,
    string? RoomAssignment,
    string? TriageNotes,
    string? Status = null
) : IRequest<Result<AppointmentResponseDto>>;
