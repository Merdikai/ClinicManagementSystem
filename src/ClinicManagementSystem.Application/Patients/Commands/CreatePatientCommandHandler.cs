using AutoMapper;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Domain.Entities;
using ClinicManagementSystem.Domain.Interfaces;
using MediatR;

namespace ClinicManagementSystem.Application.Patients.Commands;

public class CreatePatientCommandHandler : IRequestHandler<CreatePatientCommand, PatientResponseDto>
{
    private readonly IPatientRepository _patientRepository;
    private readonly IMapper _mapper;

    public CreatePatientCommandHandler(IPatientRepository patientRepository, IMapper mapper)
    {
        _patientRepository = patientRepository;
        _mapper = mapper;
    }

    public async Task<PatientResponseDto> Handle(CreatePatientCommand request, CancellationToken cancellationToken)
    {
        var dto = new CreatePatientDto(request.FirstName, request.LastName, request.DateOfBirth,
            request.Gender, request.Phone, request.Email, request.Address, request.BloodGroup, request.EmergencyContact);
        var patient = _mapper.Map<Patient>(dto);
        patient.MedicalRecordNumber = GenerateMRN();

        await _patientRepository.AddAsync(patient);
        await _patientRepository.SaveChangesAsync();
        return _mapper.Map<PatientResponseDto>(patient);
    }

    private static string GenerateMRN()
    {
        var year = DateTime.UtcNow.Year;
        var random = new Random().Next(10000, 99999);
        return $"PAT-{year}-{random}";
    }
}
