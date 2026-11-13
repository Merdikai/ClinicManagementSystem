using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.StaffDuty.DTOs;
using ClinicManagementSystem.Application.Validators;
using FluentAssertions;
using Xunit;

namespace ClinicManagementSystem.UnitTests.Validators;

public class ValidatorTests
{
    [Fact]
    public void CreatePatientValidator_ValidDto_ShouldNotHaveErrors()
    {
        var validator = new CreatePatientValidator();
        var dto = new CreatePatientDto
        {
            FirstName = "Alice",
            LastName = "Smith",
            DateOfBirth = DateTime.UtcNow.AddYears(-30),
            Gender = "Female",
            Phone = "555-1234",
            Email = "alice@example.com",
            Address = "123 Main St",
            BloodGroup = "O+",
            EmergencyContact = "Bob Smith 555-4321"
        };

        var result = validator.Validate(dto);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreatePatientValidator_InvalidFields_ShouldFailWithValidationErrors()
    {
        var validator = new CreatePatientValidator();
        var dto = new CreatePatientDto
        {
            FirstName = "",
            LastName = "",
            DateOfBirth = DateTime.UtcNow.AddDays(1), // Future DOB
            Gender = "Unknown",                       // Invalid gender
            Phone = new string('9', 25),              // Too long (>20)
            Email = "not-an-email",                   // Invalid email
            Address = "",
            BloodGroup = "Z-",                        // Invalid blood group
            EmergencyContact = ""
        };

        var result = validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.FirstName));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.LastName));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.DateOfBirth));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Gender));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Phone));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Email));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.BloodGroup));
    }

    [Fact]
    public void LoginValidator_EmptyCredentials_ShouldHaveErrors()
    {
        var validator = new LoginValidator();
        var dto = new LoginDto("", "");

        var result = validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Username));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Password));
    }

    [Fact]
    public void RegisterUserValidator_WeakPassword_ShouldHaveErrors()
    {
        var validator = new RegisterUserValidator();
        var dto = new RegisterUserDto("newuser", "user@clinic.com", "short", "John", "Doe", "+15551234567");

        var result = validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Password));
    }

    [Fact]
    public void CreateAppointmentValidator_PastDate_ShouldHaveError()
    {
        var validator = new CreateAppointmentValidator();
        var dto = new CreateAppointmentDto(
            PatientId: Guid.NewGuid(),
            DoctorId: Guid.NewGuid(),
            ScheduledDateTime: DateTime.UtcNow.AddDays(-2),
            DurationMinutes: 30,
            ReasonForVisit: "Regular Checkup"
        );

        var result = validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.ScheduledDateTime));
    }

    [Fact]
    public void CreateMedicineValidator_ValidCommand_ShouldPass()
    {
        var validator = new CreateMedicineValidator();
        var dto = new CreateMedicineDto(
            Code: "MED-100",
            Name: "Ibuprofen",
            Category: "Analgesic",
            StockQuantity: 200,
            UnitPrice: 6.50m
        );

        var result = validator.Validate(dto);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void RecordVitalsValidator_OutOfRangeVitals_ShouldFail()
    {
        var validator = new RecordVitalsValidator();
        var dto = new RecordVitalsDto(
            AppointmentId: Guid.NewGuid(),
            SystolicBP: 350,   // > 300
            DiastolicBP: 20,   // < 30
            TemperatureC: 55.0m,// > 45
            HeartRateBpm: 350, // > 300
            RespiratoryRate: 100, // > 80
            WeightKg: 0.1m,    // < 0.5
            HeightCm: 10       // < 20
        );

        var result = validator.Validate(dto);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void ClockInDutyValidator_ValidStaffRole_ShouldPass()
    {
        var validator = new ClockInDutyValidator();
        var dto = new ClockInDutyDto
        {
            Department = "Emergency",
            ShiftType = "Day",
            Notes = "On-time arrival"
        };

        var result = validator.Validate(dto);

        result.IsValid.Should().BeTrue();
    }
}
