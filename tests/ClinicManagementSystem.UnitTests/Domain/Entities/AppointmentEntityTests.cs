using ClinicManagementSystem.Domain.Entities;
using ClinicManagementSystem.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace ClinicManagementSystem.UnitTests.Domain.Entities;

public class AppointmentEntityTests
{
    [Fact]
    public void Appointment_SettingPositiveDuration_ShouldSucceed()
    {
        var appt = new Appointment
        {
            DurationMinutes = 45
        };

        appt.DurationMinutes.Should().Be(45);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-15)]
    public void Appointment_NonPositiveDuration_ShouldThrowArgumentOutOfRangeException(int duration)
    {
        var appt = new Appointment();
        var act = () => appt.DurationMinutes = duration;

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("DurationMinutes");
    }

    [Fact]
    public void Appointment_DefaultValues_ShouldBeSetCorrectly()
    {
        var appt = new Appointment();

        appt.Id.Should().NotBeEmpty();
        appt.DurationMinutes.Should().Be(30);
        appt.Status.Should().Be(AppointmentStatus.Scheduled);
        appt.VisitType.Should().Be("General Consultation");
        appt.IsDeleted.Should().BeFalse();
        appt.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
}

