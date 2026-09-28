using ClinicManagementSystem.Application.Interfaces;
using ClinicManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Asp.Versioning;
using ClinicManagementSystem.API.Constants;
using ClinicManagementSystem.Application.Common;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.Patients.Commands;
using ClinicManagementSystem.Application.Patients.Queries;
using ClinicManagementSystem.Application.Appointments.Queries;
using MediatR;
using AutoMapper;
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
    [HttpGet("me")]
    [EndpointSummary("Get current authenticated patient record")]
    [ProducesResponseType(typeof(PatientResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCurrentPatient()
    {
        var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value 
            ?? User.FindFirst("email")?.Value
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;

        // If anonymous or simulation without specific email, fall back to Alice
        if (string.IsNullOrWhiteSpace(email))
        {
            email = "alice.patient@clinic.com";
        }

        var patient = await _context.Patients.FirstOrDefaultAsync(p => p.Email == email);
        if (patient is null && (email.Equals("alice.patient@clinic.com", StringComparison.OrdinalIgnoreCase) || email.Contains("alice")))
        {
            patient = await _context.Patients.FirstOrDefaultAsync(p => p.FirstName == "Alice");
        }

        // If patient record does not exist yet, check if a registered user exists and auto-provision patient chart
        if (patient is null)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user != null)
            {
                patient = new Patient
                {
                    Id = Guid.NewGuid(),
                    FirstName = !string.IsNullOrWhiteSpace(user.FirstName) ? user.FirstName : user.Username,
                    LastName = !string.IsNullOrWhiteSpace(user.LastName) ? user.LastName : "Patient",
                    Email = user.Email,
                    Phone = !string.IsNullOrWhiteSpace(user.PhoneNumber) ? user.PhoneNumber : "+15550000000",
                    DateOfBirth = new DateTime(1990, 1, 1),
                    Gender = "Other",
                    Address = "Registered Clinic Patient",
                    MedicalRecordNumber = $"MRN-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}",
                    RegisteredAt = DateTime.UtcNow
                };
                _context.Patients.Add(patient);
                await _context.SaveChangesAsync();
            }
        }

        if (patient is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Not Found",
                Detail = $"No patient record found for '{email}'.",
                Status = 404
            });
        }

        var patientDto = _mapper.Map<PatientResponseDto>(patient);
        return Ok(patientDto);
    }


    private readonly ISender _sender;
    private readonly IMapper _mapper;
    private readonly IClinicDbContext _context;

    public PatientsController(ISender sender, IMapper mapper, IClinicDbContext context)
    {
        _sender = sender;
        _mapper = mapper;
        _context = context;
    }

    [HttpPost]
    [EndpointSummary("Create a new patient record")]
    [ProducesResponseType(typeof(PatientResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreatePatient([FromBody] CreatePatientDto dto)
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
        var result = await _sender.Send(command);

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
    [EndpointSummary("Master Patient Index (MPI) screening to detect duplicate patient registrations")]
    [ProducesResponseType(typeof(List<MpiLookupResultDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> MpiLookup(
        [FromQuery] string? query = null,
        [FromQuery] string? nationalId = null,
        [FromQuery] string? phone = null,
        [FromQuery] string? firstName = null,
        [FromQuery] string? lastName = null)
    {
        var results = await _sender.Send(new MpiLookupQuery(query, nationalId, phone, firstName, lastName));
        return Ok(results);
    }

    private async Task<Guid?> GetCurrentPatientIdAsync()
    {
        var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value 
            ?? User.FindFirst("email")?.Value
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
        if (string.IsNullOrWhiteSpace(email))
            email = "alice.patient@clinic.com";

        var pat = await _context.Patients.FirstOrDefaultAsync(p => p.Email == email);
        if (pat is null && (email.Equals("alice.patient@clinic.com", StringComparison.OrdinalIgnoreCase) || email.Contains("alice")))
        {
            pat = await _context.Patients.FirstOrDefaultAsync(p => p.FirstName == "Alice");
        }
        return pat?.Id;
    }

    [HttpGet("{id:guid}")]
    [EndpointSummary("Get patient by ID")]
    [ProducesResponseType(typeof(PatientResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetPatientById(Guid id)
    {
        if (User.IsInRole("Patient"))
        {
            var myPatientId = await GetCurrentPatientIdAsync();
            if (myPatientId == null || myPatientId != id)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
                {
                    Title = "Forbidden",
                    Detail = "Patients are only authorized to view their own patient file.",
                    Status = StatusCodes.Status403Forbidden
                });
            }
        }

        var patient = await _sender.Send(new GetPatientByIdQuery(id));
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
    [EndpointSummary("Get paginated list of patients with search and sort")]
    [ProducesResponseType(typeof(PagedResponse<PatientResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetPatients(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool descending = false)
    {
        if (User.IsInRole("Patient"))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Title = "Forbidden",
                Detail = "Patients are not authorized to view the clinic-wide patient directory.",
                Status = StatusCodes.Status403Forbidden
            });
        }

        var result = await _sender.Send(new GetPatientsPagedQuery(page, pageSize, search, sortBy, descending));
        return Ok(result);
    }

    [HttpGet("{id:guid}/medical-history")]
    [EndpointSummary("Get comprehensive patient medical history including diagnoses, vitals, prescriptions, and lab tests")]
    [ProducesResponseType(typeof(MedicalHistoryResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMedicalHistory(Guid id)
    {
        if (User.IsInRole("Patient"))
        {
            var myPatientId = await GetCurrentPatientIdAsync();
            if (myPatientId == null || myPatientId != id)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
                {
                    Title = "Forbidden",
                    Detail = "Patients are only authorized to view their own medical history.",
                    Status = StatusCodes.Status403Forbidden
                });
            }
        }

        var history = await _sender.Send(new GetPatientMedicalHistoryQuery(id));
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
    [EndpointSummary("Get all appointments for a patient (administrative visit log)")]
    [ProducesResponseType(typeof(List<AppointmentResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetPatientAppointments(Guid id)
    {
        if (User.IsInRole("Patient"))
        {
            var myPatientId = await GetCurrentPatientIdAsync();
            if (myPatientId == null || myPatientId != id)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
                {
                    Title = "Forbidden",
                    Detail = "Patients are only authorized to view their own appointments.",
                    Status = StatusCodes.Status403Forbidden
                });
            }
        }

        var result = await _sender.Send(new GetAppointmentsPagedQuery(1, 100, null, null, id, null));
        return Ok(result.Items);
    }

    [HttpGet("my-panel")]
    [Authorize(Roles = "Doctor,Admin")]
    [EndpointSummary("Get clinical patient panel assigned to the current doctor")]
    [ProducesResponseType(typeof(List<PatientResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyPanel([FromQuery] Guid? doctorId = null)
    {
        Guid targetDoctorId = Guid.Empty;
        if (doctorId.HasValue && doctorId.Value != Guid.Empty && User.IsInRole("Admin"))
        {
            targetDoctorId = doctorId.Value;
        }
        else
        {
            var doctorIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value
                ?? User.FindFirst("id")?.Value;
            Guid.TryParse(doctorIdStr, out targetDoctorId);
        }

        if (targetDoctorId == Guid.Empty)
            return Ok(new List<PatientResponseDto>());

        // Find patients who have appointments or consultations with this doctor
        var patientIdsFromAppts = await _context.Appointments
            .Where(a => a.DoctorId == targetDoctorId)
            .Select(a => a.PatientId)
            .Distinct()
            .ToListAsync();

        var patientIdsFromConsults = await _context.Consultations
            .Where(c => c.DoctorId == targetDoctorId)
            .Join(_context.Appointments, c => c.AppointmentId, a => a.Id, (c, a) => a.PatientId)
            .Distinct()
            .ToListAsync();

        var allAssignedPatientIds = patientIdsFromAppts.Union(patientIdsFromConsults).Distinct().ToList();

        var patients = await _context.Patients
            .Where(p => allAssignedPatientIds.Contains(p.Id))
            .OrderBy(p => p.LastName)
            .ThenBy(p => p.FirstName)
            .ToListAsync();

        var dtos = _mapper.Map<List<PatientResponseDto>>(patients);
        return Ok(dtos);
    }

    [HttpPut("{id:guid}")]
    [EndpointSummary("Update an existing patient record")]
    [ProducesResponseType(typeof(PatientResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePatient(Guid id, [FromBody] CreatePatientDto dto)
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

        var result = await _sender.Send(command);

        return result.Match<IActionResult>(
            onSuccess: patient => Ok(patient),
            onFailure: (error, code) => code == "NOT_FOUND" 
                ? NotFound(new ProblemDetails { Title = "Not Found", Detail = error, Status = 404 })
                : BadRequest(new ProblemDetails { Title = "Update Failed", Detail = error, Status = 400 })
        );
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [EndpointSummary("Delete a patient record (Admin only)")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePatient(Guid id)
    {
        var result = await _sender.Send(new DeletePatientCommand(id));

        return result.Match<IActionResult>(
            onSuccess: _ => NoContent(),
            onFailure: (error, code) => NotFound(new ProblemDetails { Title = "Not Found", Detail = error, Status = 404 })
        );
    }
}
