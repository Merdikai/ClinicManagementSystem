using ClinicManagementSystem.Application.DTOs;
using MediatR;

namespace ClinicManagementSystem.Application.Appointments.Commands;

using ClinicManagementSystem.Application.Common;

public record CreateAppointmentCommand(
    Guid PatientId,
    Guid DoctorId,
    DateTime ScheduledDateTime,
    int DurationMinutes,
    string ReasonForVisit
) : IRequest<Result<AppointmentResponseDto>>;
