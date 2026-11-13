using ClinicManagementSystem.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace ClinicManagementSystem.UnitTests.Domain.Entities;

public class StaffDutyShiftEntityTests
{
    [Fact]
    public void NewShift_DefaultState_ShouldBeActive()
    {
        var shift = new StaffDutyShift
        {
            UserId = Guid.NewGuid(),
            UserName = "nurse.joy",
            Role = "Nurse",
            Department = "Emergency",
            ShiftType = "Night",
            ClockInTimeUtc = DateTime.UtcNow,
            IsActive = true
        };

        shift.IsActive.Should().BeTrue();
        shift.ClockOutTimeUtc.Should().BeNull();
        shift.HandoverNotes.Should().BeNull();
    }

    [Fact]
    public void ClockOut_ShouldDeactivateShiftAndRecordHandover()
    {
        var clockIn = DateTime.UtcNow.AddHours(-8);
        var clockOut = DateTime.UtcNow;

        var shift = new StaffDutyShift
        {
            UserId = Guid.NewGuid(),
            UserName = "nurse.joy",
            Role = "Nurse",
            ClockInTimeUtc = clockIn,
            IsActive = true
        };

        shift.IsActive = false;
        shift.ClockOutTimeUtc = clockOut;
        shift.HandoverNotes = "Bed 4 patient stable; awaiting lab results.";

        shift.IsActive.Should().BeFalse();
        shift.ClockOutTimeUtc.Should().Be(clockOut);
        shift.HandoverNotes.Should().Contain("Bed 4");
    }
}

