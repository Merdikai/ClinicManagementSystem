using AutoMapper;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.Interfaces;
using ClinicManagementSystem.Application.Patients.Commands;
using ClinicManagementSystem.Domain.Entities;
using ClinicManagementSystem.Domain.Interfaces;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace ClinicManagementSystem.UnitTests.Application.Patients;

public class PatientCommandHandlerTests
{
    private readonly IPatientRepository _patientRepository = Substitute.For<IPatientRepository>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();
    private readonly ILinkGeneratorService _linkGenerator = Substitute.For<ILinkGeneratorService>();

    [Fact]
    public async Task CreatePatient_ShouldGenerateMRN_AndSavePatient()
    {
        var command = new CreatePatientCommand(
            FirstName: "Bob",
            LastName: "Builder",
            DateOfBirth: DateTime.UtcNow.AddYears(-25),
            Gender: "Male",
            Phone: "555-9876",
            Email: "bob@builder.com",
            Address: "456 Construction Rd",
            BloodGroup: "A+",
            EmergencyContact: "Wendy 555-5432"
        );

        var createdPatient = new Patient
        {
            Id = Guid.NewGuid(),
            FirstName = "Bob",
            LastName = "Builder"
        };

        var responseDto = new PatientResponseDto
        {
            Id = createdPatient.Id,
            FirstName = "Bob",
            LastName = "Builder",
            MedicalRecordNumber = "PAT-2026-12345"
        };

        _mapper.Map<Patient>(Arg.Any<CreatePatientDto>()).Returns(createdPatient);
        _mapper.Map<PatientResponseDto>(createdPatient).Returns(responseDto);
        _linkGenerator.GeneratePatientLinks(createdPatient.Id).Returns(new List<LinkDto>
        {
            new("http://localhost/api/v1/patients/" + createdPatient.Id, "self", "GET")
        });

        var handler = new CreatePatientCommandHandler(_patientRepository, _mapper, _linkGenerator);
        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.FirstName.Should().Be("Bob");
        createdPatient.MedicalRecordNumber.Should().StartWith("PAT-");

        await _patientRepository.Received(1).AddAsync(createdPatient, Arg.Any<CancellationToken>());
        await _patientRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}

