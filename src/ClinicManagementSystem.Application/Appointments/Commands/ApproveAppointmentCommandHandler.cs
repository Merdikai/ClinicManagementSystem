using System.Threading;
using System.Threading.Tasks;
using ClinicManagementSystem.Application.Common;
using ClinicManagementSystem.Domain.Enums;
using ClinicManagementSystem.Domain.Interfaces;
using MediatR;

namespace ClinicManagementSystem.Application.Appointments.Commands;

public class ApproveAppointmentCommandHandler : IRequestHandler<ApproveAppointmentCommand, Result<bool>>
{
    private readonly IAppointmentRepository _appointmentRepository;

    public ApproveAppointmentCommandHandler(IAppointmentRepository appointmentRepository)
    {
        _appointmentRepository = appointmentRepository;
    }

    public async Task<Result<bool>> Handle(ApproveAppointmentCommand request, CancellationToken cancellationToken)
    {
        var appointment = await _appointmentRepository.GetByIdAsync(request.Id);
        if (appointment == null)
            return Result<bool>.Failure("Appointment not found", "not_found");

        if (request.DoctorId.HasValue && appointment.DoctorId != Guid.Empty && appointment.DoctorId != request.DoctorId.Value && !request.IsAdmin)
        {
            return Result<bool>.Failure("Doctor Account Isolation: Only the assigned physician can approve this appointment.", "unauthorized");
        }

        appointment.Status = AppointmentStatus.Scheduled;
        await _appointmentRepository.SaveChangesAsync();

        return Result<bool>.Success(true);
    }
}
