using ClinicManagementSystem.Application.Common;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;

namespace ClinicManagementSystem.Application.Patients.Commands;

public record UpdatePatientCommand(
    Guid Id,
    string FirstName,
    string LastName,
    DateTime DateOfBirth,
    string Gender,
    string Phone,
    string? Email,
    string? Address,
    string? BloodGroup,
    string? EmergencyContact,
    string? Allergies = null,
    string? NationalId = null,
    string? InsuranceProvider = null,
    string? InsurancePolicyNumber = null,
    string? InsuranceCoveragePlan = null,
    string? InsuranceEligibilityStatus = null
) : IRequest<Result<PatientResponseDto>>;

public class UpdatePatientCommandHandler : IRequestHandler<UpdatePatientCommand, Result<PatientResponseDto>>
{
    private readonly IClinicDbContext _context;
    private readonly HybridCache _cache;

    public UpdatePatientCommandHandler(IClinicDbContext context, HybridCache cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<Result<PatientResponseDto>> Handle(UpdatePatientCommand request, CancellationToken cancellationToken)
    {
        var patient = await _context.Patients.FindAsync(new object[] { request.Id }, cancellationToken);
        if (patient == null)
        {
            return Result<PatientResponseDto>.Failure($"Patient with ID '{request.Id}' was not found.", "NOT_FOUND");
        }

        patient.FirstName = request.FirstName;
        patient.LastName = request.LastName;
        patient.DateOfBirth = request.DateOfBirth;
        patient.Gender = request.Gender;
        patient.Phone = request.Phone;
        patient.Email = request.Email ?? string.Empty;
        patient.Address = request.Address ?? string.Empty;
        patient.BloodGroup = request.BloodGroup ?? string.Empty;
        patient.EmergencyContact = request.EmergencyContact ?? "";
        patient.Allergies = request.Allergies ?? "";
        if (request.NationalId != null) patient.NationalId = request.NationalId;
        if (request.InsuranceProvider != null) patient.InsuranceProvider = request.InsuranceProvider;
        if (request.InsurancePolicyNumber != null) patient.InsurancePolicyNumber = request.InsurancePolicyNumber;
        if (request.InsuranceCoveragePlan != null) patient.InsuranceCoveragePlan = request.InsuranceCoveragePlan;
        if (request.InsuranceEligibilityStatus != null) patient.InsuranceEligibilityStatus = request.InsuranceEligibilityStatus;

        await _context.SaveChangesAsync(cancellationToken);

        try
        {
            await _cache.RemoveByTagAsync("patients", cancellationToken);
            await _cache.RemoveAsync($"patient_v1:{request.Id}", cancellationToken);
        }
        catch { }

        var dto = new PatientResponseDto
        {
            Id = patient.Id,
            MedicalRecordNumber = patient.MedicalRecordNumber,
            FirstName = patient.FirstName,
            LastName = patient.LastName,
            DateOfBirth = patient.DateOfBirth,
            Gender = patient.Gender,
            Phone = patient.Phone,
            Email = patient.Email ?? string.Empty,
            Address = patient.Address ?? string.Empty,
            BloodGroup = patient.BloodGroup ?? string.Empty,
            EmergencyContact = patient.EmergencyContact,
            Allergies = patient.Allergies,
            NationalId = patient.NationalId,
            InsuranceProvider = patient.InsuranceProvider,
            InsurancePolicyNumber = patient.InsurancePolicyNumber,
            InsuranceCoveragePlan = patient.InsuranceCoveragePlan,
            InsuranceEligibilityStatus = patient.InsuranceEligibilityStatus,
            RegisteredAt = patient.RegisteredAt
        };

        return Result<PatientResponseDto>.Success(dto);
    }
}
