using System.Threading;
using System.Threading.Tasks;
using ClinicManagementSystem.Application.Common;
using ClinicManagementSystem.Domain.Enums;
using ClinicManagementSystem.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;

namespace ClinicManagementSystem.Application.Appointments.Commands;

public class ApproveAppointmentCommandHandler : IRequestHandler<ApproveAppointmentCommand, Result<bool>>
{
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly HybridCache? _cache;

    public ApproveAppointmentCommandHandler(IAppointmentRepository appointmentRepository, HybridCache? cache = null)
    {
        _appointmentRepository = appointmentRepository;
        _cache = cache;
    }

    public async Task<Result<bool>> Handle(ApproveAppointmentCommand request, CancellationToken cancellationToken)
    {
        var appointment = await _appointmentRepository.GetByIdAsync(request.Id, cancellationToken);
        if (appointment == null)
            return Result<bool>.Failure("Appointment not found", "not_found");

        if (request.DoctorId.HasValue && appointment.DoctorId != Guid.Empty && appointment.DoctorId != request.DoctorId.Value && !request.IsAdmin)
        {
            return Result<bool>.Failure("Doctor Account Isolation: Only the assigned physician can approve this appointment.", "unauthorized");
        }

        appointment.Status = AppointmentStatus.Scheduled;
        await _appointmentRepository.SaveChangesAsync(cancellationToken);

        if (_cache is not null)
        {
            try
            {
                await _cache.RemoveByTagAsync("appointments", cancellationToken);
                await _cache.RemoveAsync($"appointment_v1:{request.Id}", cancellationToken);
            }
            catch { }
        }

        return Result<bool>.Success(true);
    }
}
