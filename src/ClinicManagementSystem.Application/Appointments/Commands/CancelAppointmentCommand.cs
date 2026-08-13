using MediatR;

namespace ClinicManagementSystem.Application.Appointments.Commands;

public record CancelAppointmentCommand(Guid Id) : IRequest;
