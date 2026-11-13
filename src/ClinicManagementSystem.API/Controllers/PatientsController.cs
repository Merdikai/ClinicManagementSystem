using System.Security.Claims;
using Asp.Versioning;
using AutoMapper;
using ClinicManagementSystem.API.Authorization;
using ClinicManagementSystem.API.Constants;
using ClinicManagementSystem.API.Filters;
using ClinicManagementSystem.Application.Appointments.Queries;
using ClinicManagementSystem.Application.Common;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.Interfaces;
using ClinicManagementSystem.Application.Patients.Commands;
using ClinicManagementSystem.Application.Patients.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClinicManagementSystem.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/patients")]
[Tags("Patients")]
[EnableRateLimiting(RateLimitingConstants.StaffPolicy)]
[Authorize]
public class PatientsController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IMapper _mapper;
    private readonly IAuthorizationService _authorizationService;
    private readonly IInsuranceVerificationClient _insuranceClient;

    public PatientsController(
        ISender sender,
        IMapper mapper,
        IAuthorizationService authorizationService,
        IInsuranceVerificationClient insuranceClient)
    {
        _sender = sender;
        _mapper = mapper;
        _authorizationService = authorizationService;
        _insuranceClient = insuranceClient;
    }

    [HttpPost("verify-insurance")]
    [Authorize(Roles = "Admin,Receptionist,Doctor,Nurse")]
    [EndpointSummary("Verify patient insurance eligibility with clearinghouse resilience client")]
    [ProducesResponseType(typeof(InsuranceVerificationResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> VerifyInsurance([FromBody] InsuranceVerificationRequest request, CancellationToken ct = default)
    {
        var result = await _insuranceClient.VerifyEligibilityAsync(request, ct);
        return Ok(result);
    }

    [HttpGet("me")]
    [EndpointSummary("Get current authenticated patient record")]
    [ProducesResponseType(typeof(PatientResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCurrentPatient(CancellationToken ct = default)
    {
        var email = User.FindFirst(ClaimTypes.Email)?.Value
            ?? User.FindFirst("email")?.Value;
        var username = User.FindFirst(ClaimTypes.Name)?.Value;

        if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(username))
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Unauthorized",
                Detail = "Authenticated user identity claims are missing.",
                Status = StatusCodes.Status401Unauthorized
            });
        }

        var patientDto = await _sender.Send(new GetCurrentPatientQuery(email, username), ct);
        if (patientDto is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Not Found",
                Detail = "No patient record found for authenticated user.",
                Status = 404
            });
        }

        return Ok(patientDto);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Receptionist,Doctor,Nurse")]
    [EndpointSummary("Create a new patient record")]
    [ProducesResponseType(typeof(PatientResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreatePatient([FromBody] CreatePatientDto dto, CancellationToken ct)
    {
        var command = new CreatePatientCommand(
            dto.FirstName,
            dto.LastName,
            dto.DateOfBirth,
            dto.Gender,
            dto.Phone,
            dto.Email ?? string.Empty,
            dto.Address ?? string.Empty,
            dto.BloodGroup ?? string.Empty,
            dto.EmergencyContact ?? string.Empty,
            dto.Allergies ?? string.Empty,
            dto.NationalId,
            dto.InsuranceProvider,
            dto.InsurancePolicyNumber,
            dto.InsuranceCoveragePlan,
            dto.InsuranceEligibilityStatus
        );
        var result = await _sender.Send(command, ct);

        return result.Match<IActionResult>(
            onSuccess: patient =>
            {
                var patientDto = _mapper.Map<PatientResponseDto>(patient);
                return CreatedAtAction(
                    nameof(GetPatientById),
                    new { id = patient.Id, version = "1.0" },
                    patientDto);
            },
            onFailure: (error, code) => BadRequest(new ProblemDetails
            {
                Title = "Patient Creation Failed",
                Detail = error,
                Status = 400
            })
        );
    }

    [HttpGet("mpi-lookup")]
    [Authorize(Roles = "Admin,Receptionist,Doctor,Nurse")]
    [EndpointSummary("Master Patient Index (MPI) screening to detect duplicate patient registrations")]
    [ProducesResponseType(typeof(List<MpiLookupResultDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> MpiLookup(
        [FromQuery] string? query = null,
        [FromQuery] string? nationalId = null,
        [FromQuery] string? phone = null,
        [FromQuery] string? firstName = null,
        [FromQuery] string? lastName = null,
        CancellationToken ct = default)
    {
        var results = await _sender.Send(new MpiLookupQuery(query, nationalId, phone, firstName, lastName), ct);
        return Ok(results);
    }

    [HttpGet("deleted")]
    [Authorize(Roles = "Admin")]
    [EndpointSummary("List soft-deleted patients for Admin recovery")]
    [ProducesResponseType(typeof(List<PatientResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDeletedPatients(CancellationToken ct = default)
    {
        var deleted = await _sender.Send(new GetDeletedPatientsQuery(), ct);
        return Ok(deleted);
    }

    [HttpPost("{id:guid}/restore")]
    [Authorize(Roles = "Admin")]
    [AuditAction("Patient", "Restored")]
    [EndpointSummary("Restore a soft-deleted patient record (Admin only)")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RestorePatient(Guid id, CancellationToken ct = default)
    {
        var result = await _sender.Send(new RestorePatientCommand(id), ct);
        return result.Match<IActionResult>(
            onSuccess: _ => NoContent(),
            onFailure: (error, _) => NotFound(new ProblemDetails { Title = "Not Found", Detail = error, Status = 404 })
        );
    }

    [HttpGet("{id:guid}")]
    [ETagFilter]
    [Authorize(Roles = "Admin,Doctor,Nurse,Receptionist,Pharmacist,LabTechnician,Radiologist,Accountant,Patient")]
    [EndpointSummary("Get patient by ID")]
    [ProducesResponseType(typeof(PatientResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetPatientById(Guid id, CancellationToken ct)
    {
        var authResult = await _authorizationService.AuthorizeAsync(User, id, ClinicResourceOperationRequirement.Read);
        if (!authResult.Succeeded)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Title = "Forbidden",
                Detail = "Patients are only authorized to view their own patient file.",
                Status = StatusCodes.Status403Forbidden
            });
        }

        var patient = await _sender.Send(new GetPatientByIdQuery(id), ct);
        if (patient is null)
            return NotFound(new ProblemDetails
            {
                Title = "Not Found",
                Detail = $"Patient with ID '{id}' was not found.",
                Status = 404
            });

        var patientDto = _mapper.Map<PatientResponseDto>(patient);
        return Ok(patientDto);
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Doctor,Nurse,Receptionist,Pharmacist,LabTechnician,Radiologist,Accountant")]
    [EndpointSummary("Get paginated list of patients with search and sort")]
    [ProducesResponseType(typeof(PagedResponse<PatientResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetPatients(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool descending = false,
        CancellationToken ct = default)
    {
        var result = await _sender.Send(new GetPatientsPagedQuery(page, pageSize, search, sortBy, descending), ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}/medical-history")]
    [Authorize(Roles = "Admin,Doctor,Nurse,Receptionist,Pharmacist,LabTechnician,Radiologist,Patient")]
    [EndpointSummary("Get comprehensive patient medical history including diagnoses, vitals, prescriptions, and lab tests")]
    [ProducesResponseType(typeof(MedicalHistoryResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMedicalHistory(Guid id, CancellationToken ct)
    {
        var authResult = await _authorizationService.AuthorizeAsync(User, id, ClinicResourceOperationRequirement.Read);
        if (!authResult.Succeeded)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Title = "Forbidden",
                Detail = "Patients are only authorized to view their own medical history.",
                Status = StatusCodes.Status403Forbidden
            });
        }

        var history = await _sender.Send(new GetPatientMedicalHistoryQuery(id), ct);
        if (history is null)
            return NotFound(new ProblemDetails
            {
                Title = "Not Found",
                Detail = $"Patient medical history for ID '{id}' was not found.",
                Status = 404
            });

        return Ok(history);
    }

    [HttpGet("{id:guid}/appointments")]
    [Authorize(Roles = "Admin,Doctor,Nurse,Receptionist,Patient")]
    [EndpointSummary("Get all appointments for a patient (administrative visit log)")]
    [ProducesResponseType(typeof(List<AppointmentResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetPatientAppointments(Guid id, CancellationToken ct)
    {
        var authResult = await _authorizationService.AuthorizeAsync(User, id, ClinicResourceOperationRequirement.Read);
        if (!authResult.Succeeded)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Title = "Forbidden",
                Detail = "Patients are only authorized to view their own appointments.",
                Status = StatusCodes.Status403Forbidden
            });
        }

        var result = await _sender.Send(new GetAppointmentsPagedQuery(1, 100, null, null, id, null), ct);
        return Ok(result.Items);
    }

    [HttpGet("my-panel")]
    [Authorize(Roles = "Doctor,Admin")]
    [EndpointSummary("Get clinical patient panel assigned to the current doctor")]
    [ProducesResponseType(typeof(List<PatientResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyPanel([FromQuery] Guid? doctorId = null, CancellationToken ct = default)
    {
        Guid targetDoctorId = Guid.Empty;
        if (doctorId.HasValue && doctorId.Value != Guid.Empty && User.IsInRole("Admin"))
        {
            targetDoctorId = doctorId.Value;
        }
        else
        {
            var doctorIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value
                ?? User.FindFirst("id")?.Value;
            Guid.TryParse(doctorIdStr, out targetDoctorId);
        }

        var dtos = await _sender.Send(new GetDoctorPanelQuery(targetDoctorId), ct);
        return Ok(dtos);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,Receptionist,Doctor,Nurse")]
    [EndpointSummary("Update an existing patient record")]
    [ProducesResponseType(typeof(PatientResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePatient(Guid id, [FromBody] CreatePatientDto dto, CancellationToken ct)
    {
        var command = new UpdatePatientCommand(
            id,
            dto.FirstName,
            dto.LastName,
            dto.DateOfBirth,
            dto.Gender,
            dto.Phone,
            dto.Email,
            dto.Address,
            dto.BloodGroup,
            dto.EmergencyContact,
            dto.Allergies,
            dto.NationalId,
            dto.InsuranceProvider,
            dto.InsurancePolicyNumber,
            dto.InsuranceCoveragePlan,
            dto.InsuranceEligibilityStatus
        );

        var result = await _sender.Send(command, ct);

        return result.Match<IActionResult>(
            onSuccess: patient => Ok(patient),
            onFailure: (error, code) => code == "NOT_FOUND"
                ? NotFound(new ProblemDetails { Title = "Not Found", Detail = error, Status = 404 })
                : BadRequest(new ProblemDetails { Title = "Update Failed", Detail = error, Status = 400 })
        );
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [AuditAction("Patient", "Deleted")]
    [EndpointSummary("Soft-delete a patient record (Admin only)")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePatient(Guid id, CancellationToken ct = default)
    {
        var result = await _sender.Send(new DeletePatientCommand(id), ct);

        return result.Match<IActionResult>(
            onSuccess: _ => NoContent(),
            onFailure: (error, code) => NotFound(new ProblemDetails { Title = "Not Found", Detail = error, Status = 404 })
        );
    }
}
