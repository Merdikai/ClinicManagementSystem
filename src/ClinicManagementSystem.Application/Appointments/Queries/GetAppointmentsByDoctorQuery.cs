using ClinicManagementSystem.Application.DTOs;
using MediatR;

namespace ClinicManagementSystem.Application.Appointments.Queries;

public record GetAppointmentsByDoctorQuery(Guid DoctorId, DateTime? Date) : IRequest<IEnumerable<AppointmentResponseDto>>;
