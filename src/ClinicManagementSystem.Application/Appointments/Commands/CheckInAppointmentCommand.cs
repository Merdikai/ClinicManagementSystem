using MediatR;

namespace ClinicManagementSystem.Application.Appointments.Commands;

public record CheckInAppointmentCommand(Guid Id) : IRequest;
