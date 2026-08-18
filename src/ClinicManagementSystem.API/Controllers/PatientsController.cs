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

        var patient = await _sender.Send(command);
        patient.Links = _linkGenerator.GeneratePatientLinks(patient.Id);
        return CreatedAtAction(nameof(GetById), new { id = patient.Id }, patient);
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
        var patients = await _sender.Send(query);

        if (!string.IsNullOrWhiteSpace(fields))
        {
            var shapedItems = patients.Items.ShapeData(fields);
            return Ok(new
            {
                Items = shapedItems,
                patients.TotalCount,
                patients.Page,
                patients.PageSize,
                patients.TotalPages,
                patients.HasNext,
                patients.HasPrevious
            });
        }
        return Ok(patients);
    }

    [HttpGet("{id:guid}/appointments")]
    [EndpointSummary("Get all appointments for a patient")]
    [ProducesResponseType(typeof(IEnumerable<AppointmentResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPatientAppointments(Guid id)
    {
        var dtos = await _sender.Send(new GetPatientAppointmentsQuery(id));
        return Ok(dtos);
    }

    [HttpGet("{id:guid}/medical-history")]
    [Authorize(Roles = "Doctor,Nurse,Admin")]
    [EndpointSummary("Get full medical visit history for a patient")]
    [ProducesResponseType(typeof(MedicalHistoryResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMedicalHistory(Guid id)
    {
        var result = await _sender.Send(new GetPatientMedicalHistoryQuery(id));

        return result.Match<IActionResult>(
            onSuccess: history => Ok(history),
            onFailure: (error, errorCode) => errorCode switch
            {
                "patient_not_found" => NotFound(new ProblemDetails
                {
                    Title = "Not Found",
                    Detail = error,
                    Status = StatusCodes.Status404NotFound
                }),
                _ => BadRequest(new ProblemDetails
                {
                    Title = "Error",
                    Detail = error,
                    Status = StatusCodes.Status400BadRequest
                })
            }
        );
    }
}

