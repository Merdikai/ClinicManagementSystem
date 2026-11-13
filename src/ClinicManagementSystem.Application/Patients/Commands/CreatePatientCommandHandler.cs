using AutoMapper;
using ClinicManagementSystem.Application.Common;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.Interfaces;
using ClinicManagementSystem.Domain.Entities;
using ClinicManagementSystem.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;

namespace ClinicManagementSystem.Application.Patients.Commands;

#pragma warning disable EXTEXP0018
public class CreatePatientCommandHandler : IRequestHandler<CreatePatientCommand, Result<PatientResponseDto>>
{
    private readonly IPatientRepository _patientRepository;
    private readonly IMapper _mapper;
    private readonly ILinkGeneratorService _linkGenerator;
    private readonly HybridCache? _cache;

    public CreatePatientCommandHandler(
        IPatientRepository patientRepository,
        IMapper mapper,
        ILinkGeneratorService linkGenerator,
        HybridCache? cache = null)
    {
        _patientRepository = patientRepository;
        _mapper = mapper;
        _linkGenerator = linkGenerator;
        _cache = cache;
    }

    public async Task<Result<PatientResponseDto>> Handle(CreatePatientCommand request, CancellationToken cancellationToken)
    {
        var dto = new CreatePatientDto(request.FirstName, request.LastName, request.DateOfBirth,
            request.Gender, request.Phone, request.Email, request.Address, request.BloodGroup, request.EmergencyContact)
        {
            Allergies = request.Allergies,
            NationalId = request.NationalId,
            InsuranceProvider = request.InsuranceProvider,
            InsurancePolicyNumber = request.InsurancePolicyNumber,
            InsuranceCoveragePlan = request.InsuranceCoveragePlan,
            InsuranceEligibilityStatus = !string.IsNullOrWhiteSpace(request.InsuranceEligibilityStatus)
                ? request.InsuranceEligibilityStatus
                : (!string.IsNullOrWhiteSpace(request.InsuranceProvider) ? "Active" : "Self-Pay")
        };
        var patient = _mapper.Map<Patient>(dto);
        patient.MedicalRecordNumber = GenerateMRN();
        patient.NationalId = dto.NationalId;
        patient.InsuranceProvider = dto.InsuranceProvider;
        patient.InsurancePolicyNumber = dto.InsurancePolicyNumber;
        patient.InsuranceCoveragePlan = dto.InsuranceCoveragePlan;
        patient.InsuranceEligibilityStatus = dto.InsuranceEligibilityStatus;

        await _patientRepository.AddAsync(patient, cancellationToken);
        await _patientRepository.SaveChangesAsync(cancellationToken);

        if (_cache is not null)
        {
            try
            {
                await _cache.RemoveByTagAsync("patients", cancellationToken);
            }
            catch { }
        }

        var responseDto = _mapper.Map<PatientResponseDto>(patient);
        responseDto.Links = _linkGenerator.GeneratePatientLinks(patient.Id);
        return Result<PatientResponseDto>.Success(responseDto);
    }

    private static string GenerateMRN()
    {
        var year = DateTime.UtcNow.Year;
        var random = Random.Shared.Next(10000, 99999);
        return $"PAT-{year}-{random}";
    }
}
#pragma warning restore EXTEXP0018
