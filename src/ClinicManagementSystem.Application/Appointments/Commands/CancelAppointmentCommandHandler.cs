using ClinicManagementSystem.Application.Exceptions;
using ClinicManagementSystem.Domain.Entities;
using ClinicManagementSystem.Domain.Enums;
using ClinicManagementSystem.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;

namespace ClinicManagementSystem.Application.Appointments.Commands;

public class CancelAppointmentCommandHandler : IRequestHandler<CancelAppointmentCommand>
{
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly HybridCache? _cache;

    public CancelAppointmentCommandHandler(IAppointmentRepository appointmentRepository, HybridCache? cache = null)
    {
        _appointmentRepository = appointmentRepository;
        _cache = cache;
    }

    public async Task Handle(CancelAppointmentCommand request, CancellationToken cancellationToken)
    {
        var appointment = await _appointmentRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Appointment), request.Id);

        appointment.Status = AppointmentStatus.Cancelled;
        await _appointmentRepository.SoftDeleteAsync(request.Id, cancellationToken);
        _appointmentRepository.Update(appointment);
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
    }
}
