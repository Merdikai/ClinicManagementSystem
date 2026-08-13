using ClinicManagementSystem.Application.DTOs;
using MediatR;

namespace ClinicManagementSystem.Application.Appointments.Queries;

public record GetAppointmentByIdQuery(Guid Id) : IRequest<AppointmentResponseDto>;
