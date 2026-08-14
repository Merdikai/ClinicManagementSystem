using AutoMapper;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Domain.Entities;
using ClinicManagementSystem.Domain.Interfaces;
using MediatR;

namespace ClinicManagementSystem.Application.Patients.Commands;

using ClinicManagementSystem.Application.Interfaces;

public class CreatePatientCommandHandler : IRequestHandler<CreatePatientCommand, PatientResponseDto>
{
    private readonly IPatientRepository _patientRepository;
    private readonly IMapper _mapper;
    private readonly ILinkGeneratorService _linkGenerator;

    public CreatePatientCommandHandler(IPatientRepository patientRepository, IMapper mapper, ILinkGeneratorService linkGenerator)
    {
        _patientRepository = patientRepository;
        _mapper = mapper;
        _linkGenerator = linkGenerator;
    }

    public async Task<PatientResponseDto> Handle(CreatePatientCommand request, CancellationToken cancellationToken)
    {
        var dto = new CreatePatientDto(request.FirstName, request.LastName, request.DateOfBirth,
            request.Gender, request.Phone, request.Email, request.Address, request.BloodGroup, request.EmergencyContact);
        var patient = _mapper.Map<Patient>(dto);
        patient.MedicalRecordNumber = GenerateMRN();

        await _patientRepository.AddAsync(patient);
        await _patientRepository.SaveChangesAsync();
        var responseDto = _mapper.Map<PatientResponseDto>(patient);
        responseDto.Links = _linkGenerator.GeneratePatientLinks(patient.Id);
        return responseDto;
    }

    private static string GenerateMRN()
    {
        var year = DateTime.UtcNow.Year;
        var random = new Random().Next(10000, 99999);
        return $"PAT-{year}-{random}";
    }
}
