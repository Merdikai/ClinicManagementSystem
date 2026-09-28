using MediatR;
using ClinicManagementSystem.Application.Common;

namespace ClinicManagementSystem.Application.Appointments.Commands;

public record ApproveAppointmentCommand(Guid Id, Guid? DoctorId = null, bool IsAdmin = false) : IRequest<Result<bool>>;
