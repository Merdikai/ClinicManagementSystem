using ClinicManagementSystem.Application.Appointments.Commands;
using ClinicManagementSystem.Application.Exceptions;
using ClinicManagementSystem.Domain.Entities;
using ClinicManagementSystem.Domain.Enums;
using ClinicManagementSystem.Domain.Interfaces;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace ClinicManagementSystem.UnitTests.Application.Appointments;

public class AppointmentCommandHandlerTests
{
    private readonly IAppointmentRepository _appointmentRepository = Substitute.For<IAppointmentRepository>();

    [Fact]
    public async Task ApproveAppointment_WhenValid_ShouldSetStatusScheduled()
    {
        var apptId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var appointment = new Appointment
        {
            Id = apptId,
            DoctorId = doctorId,
            Status = AppointmentStatus.PendingApproval
        };

        _appointmentRepository.GetByIdAsync(apptId, Arg.Any<CancellationToken>()).Returns(appointment);

        var handler = new ApproveAppointmentCommandHandler(_appointmentRepository);
        var command = new ApproveAppointmentCommand(apptId, DoctorId: doctorId, IsAdmin: false);
        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        appointment.Status.Should().Be(AppointmentStatus.Scheduled);
        await _appointmentRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ApproveAppointment_WhenDifferentDoctorAndNotAdmin_ShouldReturnUnauthorized()
    {
        var apptId = Guid.NewGuid();
        var doctorA = Guid.NewGuid();
        var doctorB = Guid.NewGuid();
        var appointment = new Appointment
        {
            Id = apptId,
            DoctorId = doctorA,
            Status = AppointmentStatus.PendingApproval
        };

        _appointmentRepository.GetByIdAsync(apptId, Arg.Any<CancellationToken>()).Returns(appointment);

        var handler = new ApproveAppointmentCommandHandler(_appointmentRepository);
        var command = new ApproveAppointmentCommand(apptId, DoctorId: doctorB, IsAdmin: false);
        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("unauthorized");
        appointment.Status.Should().Be(AppointmentStatus.PendingApproval);
    }

    [Fact]
    public async Task ApproveAppointment_WhenNotFound_ShouldReturnNotFoundFailure()
    {
        var apptId = Guid.NewGuid();
        _appointmentRepository.GetByIdAsync(apptId, Arg.Any<CancellationToken>()).Returns((Appointment?)null);

        var handler = new ApproveAppointmentCommandHandler(_appointmentRepository);
        var result = await handler.Handle(new ApproveAppointmentCommand(apptId), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("not_found");
    }

    [Fact]
    public async Task CancelAppointment_WhenExists_ShouldSoftDeleteAndSave()
    {
        var apptId = Guid.NewGuid();
        var appointment = new Appointment { Id = apptId, Status = AppointmentStatus.Scheduled };

        _appointmentRepository.GetByIdAsync(apptId, Arg.Any<CancellationToken>()).Returns(appointment);

        var handler = new CancelAppointmentCommandHandler(_appointmentRepository);
        await handler.Handle(new CancelAppointmentCommand(apptId), CancellationToken.None);

        appointment.Status.Should().Be(AppointmentStatus.Cancelled);
        await _appointmentRepository.Received(1).SoftDeleteAsync(apptId, Arg.Any<CancellationToken>());
        await _appointmentRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CancelAppointment_WhenNotFound_ShouldThrowNotFoundException()
    {
        var apptId = Guid.NewGuid();
        _appointmentRepository.GetByIdAsync(apptId, Arg.Any<CancellationToken>()).Returns((Appointment?)null);

        var handler = new CancelAppointmentCommandHandler(_appointmentRepository);
        var act = async () => await handler.Handle(new CancelAppointmentCommand(apptId), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}

