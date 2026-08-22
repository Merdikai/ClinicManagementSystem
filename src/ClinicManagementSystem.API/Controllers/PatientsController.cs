using AutoMapper;
using ClinicManagementSystem.Application.Common;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.Interfaces;
using ClinicManagementSystem.Application.Patients.Commands;
using ClinicManagementSystem.Application.Patients.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using ClinicManagementSystem.API.Constants;
using Microsoft.AspNetCore.RateLimiting;

namespace ClinicManagementSystem.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/patients")]
[EnableRateLimiting(RateLimitingConstants.PatientPolicy)]
public class PatientsController : ControllerBase
{
    private readonly ISender _sender;
    private readonly ILinkGeneratorService _linkGenerator;

    public PatientsController(
        ISender sender,
        ILinkGeneratorService linkGenerator)
    {
        _sender = sender;
        _linkGenerator = linkGenerator;
    }

    [HttpPost]
    [EndpointSummary("Create a new patient")]
    [ProducesResponseType(typeof(PatientResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreatePatientDto dto)
    {
        var command = new CreatePatientCommand(
            dto.FirstName,
            dto.LastName,
            dto.DateOfBirth,
            dto.Gender,
            dto.Phone,
            dto.Email,
            dto.Address,
            dto.BloodGroup,
            dto.EmergencyContact
        );

        var result = await _sender.Send(command);
        return result.Match<IActionResult>(
            onSuccess: patient => {
                patient.Links = _linkGenerator.GeneratePatientLinks(patient.Id);
                return CreatedAtAction(nameof(GetById), new { id = patient.Id }, patient);
            },
            onFailure: (error, code) => BadRequest(new ProblemDetails { Title = "Patient Creation Failed", Detail = error, Status = 400 })
        );
    }

    [HttpGet("{id:guid}")]
    [EndpointSummary("Get a patient by ID (supports ?fields=id,firstName)")]
    [ProducesResponseType(typeof(PatientResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, [FromQuery] string? fields = null)
    {
        var patient = await _sender.Send(new GetPatientByIdQuery(id));
        if (patient is null) return NotFound(new ProblemDetails { Title = "Not Found", Detail = $"Patient {id} not found", Status = StatusCodes.Status404NotFound });

        if (!string.IsNullOrWhiteSpace(fields))
        {
            var shaped = patient.ShapeData(fields);
            return Ok(shaped);
        }

        patient.Links = _linkGenerator.GeneratePatientLinks(patient.Id);
        return Ok(patient);
    }

    [HttpGet]
    [EndpointSummary("Get paginated patients list (supports ?fields=id,firstName)")]
    [ProducesResponseType(typeof(PagedResponse<PatientResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null, [FromQuery] string? sortBy = null, [FromQuery] bool descending = false, [FromQuery] string? fields = null)
    {
        var query = new GetPatientsPagedQuery(page, pageSize, search, sortBy, descending);
        var pagedPatients = await _sender.Send(query);

        if (!string.IsNullOrWhiteSpace(fields))
        {
            var shapedItems = pagedPatients.Items.Select(p => p.ShapeData(fields)).ToList();
            return Ok(new
            {
                pagedPatients.TotalCount,
                pagedPatients.Page,
                pagedPatients.PageSize,
                pagedPatients.TotalPages,
                pagedPatients.HasNext,
                pagedPatients.HasPrevious,
                Items = shapedItems
            });
        }

        foreach (var p in pagedPatients.Items)
        {
            p.Links = _linkGenerator.GeneratePatientLinks(p.Id);
        }

        return Ok(pagedPatients);
    }

    [HttpPut("{id:guid}")]
    [EndpointSummary("Update an existing patient")]
    [ProducesResponseType(typeof(PatientResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePatient(Guid id, [FromBody] CreatePatientDto dto, [FromServices] IClinicDbContext context)
    {
        var patient = await context.Patients.FindAsync(id);
        if (patient == null) return NotFound(new ProblemDetails { Title = "Not Found", Detail = $"Patient {id} not found", Status = 404 });

        if (!string.IsNullOrEmpty(dto.FirstName)) patient.FirstName = dto.FirstName;
        if (!string.IsNullOrEmpty(dto.LastName)) patient.LastName = dto.LastName;
        if (!string.IsNullOrEmpty(dto.Phone)) patient.Phone = dto.Phone;
        if (!string.IsNullOrEmpty(dto.Email)) patient.Email = dto.Email;
        if (!string.IsNullOrEmpty(dto.Address)) patient.Address = dto.Address;
        if (!string.IsNullOrEmpty(dto.EmergencyContact)) patient.EmergencyContact = dto.EmergencyContact;
        await context.SaveChangesAsync();

        return Ok(new PatientResponseDto {
            Id = patient.Id,
            MedicalRecordNumber = patient.MedicalRecordNumber,
            FirstName = patient.FirstName,
            LastName = patient.LastName,
            DateOfBirth = patient.DateOfBirth,
            Gender = patient.Gender,
            Phone = patient.Phone,
            Email = patient.Email,
            BloodGroup = patient.BloodGroup,
            RegisteredAt = patient.RegisteredAt
        });
    }

    [HttpDelete("{id:guid}")]
    [EndpointSummary("Delete a patient")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePatient(Guid id, [FromServices] IClinicDbContext context)
    {
        var patient = await context.Patients.FindAsync(id);
        if (patient == null) return NotFound(new ProblemDetails { Title = "Not Found", Detail = $"Patient {id} not found", Status = 404 });

        context.Patients.Remove(patient);
        await context.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("{id:guid}/appointments")]
    [EndpointSummary("Get appointment history for a patient")]
    [ProducesResponseType(typeof(IEnumerable<AppointmentResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAppointments(Guid id)
    {
        var appointments = await _sender.Send(new GetPatientAppointmentsQuery(id));
        return Ok(appointments);
    }

    [HttpGet("{id:guid}/medical-history")]
    [EndpointSummary("Get full medical history for a patient")]
    [ProducesResponseType(typeof(MedicalHistoryResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMedicalHistory(Guid id)
    {
        var history = await _sender.Send(new GetPatientMedicalHistoryQuery(id));
        return Ok(history);
    }
}
