using ClinicManagementSystem.Application.Common;
using ClinicManagementSystem.Application.DTOs;
using MediatR;

namespace ClinicManagementSystem.Application.Patients.Commands;

public record CreatePatientCommand(
    string FirstName,
    string LastName,
    DateTime DateOfBirth,
    string Gender,
    string Phone,
    string Email,
    string Address,
    string BloodGroup,
    string EmergencyContact,
    string Allergies = "",
    string? NationalId = null,
    string? InsuranceProvider = null,
    string? InsurancePolicyNumber = null,
    string? InsuranceCoveragePlan = null,
    string? InsuranceEligibilityStatus = null
) : IRequest<Result<PatientResponseDto>>;
