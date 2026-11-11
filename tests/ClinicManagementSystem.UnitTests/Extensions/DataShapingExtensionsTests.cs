using ClinicManagementSystem.Application.Extensions;
using FluentAssertions;
using Xunit;

namespace ClinicManagementSystem.UnitTests.Extensions;

public class DataShapingExtensionsTests
{
    private class TestPatientDto
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string FirstName { get; set; } = "John";
        public string LastName { get; set; } = "Doe";
        public string Email { get; set; } = "john.doe@clinic.com";
        public string Phone { get; set; } = "555-0199";
    }

    [Fact]
    public void ShapeSingle_WithNullOrEmptyFields_ReturnsAllProperties()
    {
        var patient = new TestPatientDto();

        var result = patient.ShapeSingle(null);

        result.Should().NotBeNull();
        result.ContainsKey("Id").Should().BeTrue();
        result.ContainsKey("FirstName").Should().BeTrue();
        result.ContainsKey("LastName").Should().BeTrue();
        result.ContainsKey("Email").Should().BeTrue();
        result.ContainsKey("Phone").Should().BeTrue();
    }

    [Fact]
    public void ShapeSingle_WithSpecificFields_ReturnsOnlyRequestedProperties()
    {
        var patient = new TestPatientDto();

        var result = patient.ShapeSingle("FirstName,Email");

        result.Should().HaveCount(2);
        result.ContainsKey("FirstName").Should().BeTrue();
        result.ContainsKey("Email").Should().BeTrue();
        result["FirstName"].Should().Be("John");
        result["Email"].Should().Be("john.doe@clinic.com");
        result.ContainsKey("LastName").Should().BeFalse();
        result.ContainsKey("Phone").Should().BeFalse();
    }

    [Fact]
    public void ShapeData_Collection_ProjectsEachItem()
    {
        var patients = new List<TestPatientDto>
        {
            new() { FirstName = "Alice", LastName = "Smith", Email = "alice@clinic.com" },
            new() { FirstName = "Bob", LastName = "Jones", Email = "bob@clinic.com" }
        };

        var results = patients.ShapeData("FirstName,LastName").ToList();

        results.Should().HaveCount(2);
        var first = results[0];
        first.ContainsKey("FirstName").Should().BeTrue();
        first["FirstName"].Should().Be("Alice");
        first["LastName"].Should().Be("Smith");
        first.ContainsKey("Email").Should().BeFalse();

        var second = results[1];
        second.ContainsKey("FirstName").Should().BeTrue();
        second["FirstName"].Should().Be("Bob");
        second["LastName"].Should().Be("Jones");
    }

    [Fact]
    public void ShapeSingle_CaseInsensitiveFields_MatchesCorrectProperties()
    {
        var patient = new TestPatientDto();

        var result = patient.ShapeSingle("firstname,EMAIL");

        result.ContainsKey("FirstName").Should().BeTrue();
        result.ContainsKey("Email").Should().BeTrue();
    }
}
