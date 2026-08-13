using ClinicManagementSystem.Application.Exceptions;
using ClinicManagementSystem.Domain.Entities;
using ClinicManagementSystem.Domain.Enums;
using ClinicManagementSystem.Domain.Interfaces;
using MediatR;

namespace ClinicManagementSystem.Application.Appointments.Commands;

public class CheckInAppointmentCommandHandler : IRequestHandler<CheckInAppointmentCommand>
{
    private readonly IAppointmentRepository _appointmentRepository;

    public CheckInAppointmentCommandHandler(IAppointmentRepository appointmentRepository)
        => _appointmentRepository = appointmentRepository;

    public async Task Handle(CheckInAppointmentCommand request, CancellationToken cancellationToken)
    {
        var appointment = await _appointmentRepository.GetByIdAsync(request.Id)
            ?? throw new NotFoundException(nameof(Appointment), request.Id);

        if (appointment.Status != AppointmentStatus.Scheduled)
            throw new BusinessRuleViolationException("Only scheduled appointments can be checked in.", "invalid_status");

        appointment.Status = AppointmentStatus.CheckedIn;
        _appointmentRepository.Update(appointment);
        await _appointmentRepository.SaveChangesAsync();
    }
}
