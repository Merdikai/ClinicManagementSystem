using ClinicManagementSystem.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace ClinicManagementSystem.UnitTests.Domain.Entities;

public class PatientEntityTests
{
    [Fact]
    public void Patient_PropertySetters_ShouldTrimStringValues()
    {
        var patient = new Patient
        {
            FirstName = "  Alice  ",
            LastName = "  Smith  ",
            MedicalRecordNumber = "  MRN-12345  ",
            Email = "  alice@example.com  "
        };

        patient.FirstName.Should().Be("Alice");
        patient.LastName.Should().Be("Smith");
        patient.MedicalRecordNumber.Should().Be("MRN-12345");
        patient.Email.Should().Be("alice@example.com");
    }

    [Fact]
    public void Patient_DefaultValues_ShouldBeInitializedProperly()
    {
        var patient = new Patient();

        patient.Id.Should().NotBeEmpty();
        patient.IsDeleted.Should().BeFalse();
        patient.DeletedAt.Should().BeNull();
        patient.RegisteredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        patient.InsuranceEligibilityStatus.Should().Be("SelfPay");
        patient.MedicalRecords.Should().NotBeNull().And.BeEmpty();
        patient.Appointments.Should().NotBeNull().And.BeEmpty();
        patient.Invoices.Should().NotBeNull().And.BeEmpty();
    }
}

