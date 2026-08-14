using ClinicManagementSystem.Application.Exceptions;
using ClinicManagementSystem.Domain.Entities;
using ClinicManagementSystem.Domain.Enums;
using ClinicManagementSystem.Domain.Interfaces;
using MediatR;

namespace ClinicManagementSystem.Application.Appointments.Commands;

public class CancelAppointmentCommandHandler : IRequestHandler<CancelAppointmentCommand>
{
    private readonly IAppointmentRepository _appointmentRepository;

    public CancelAppointmentCommandHandler(IAppointmentRepository appointmentRepository)
        => _appointmentRepository = appointmentRepository;

    public async Task Handle(CancelAppointmentCommand request, CancellationToken cancellationToken)
    {
        var appointment = await _appointmentRepository.GetByIdAsync(request.Id)
            ?? throw new NotFoundException(nameof(Appointment), request.Id);

        appointment.Status = AppointmentStatus.Cancelled;
        await _appointmentRepository.SoftDeleteAsync(request.Id);
        _appointmentRepository.Update(appointment);
        await _appointmentRepository.SaveChangesAsync();
    }
}
