using AutoMapper;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.Patients.Commands;
using ClinicManagementSystem.Application.Patients.Queries;
using MediatR;
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
    private readonly IMapper _mapper;

    public PatientsController(ISender sender, IMapper mapper)
    {
        _sender = sender;
        _mapper = mapper;
    }

    [HttpPost]
    [EndpointSummary("Create a new patient")]
    [ProducesResponseType(typeof(PatientResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreatePatientDto dto)
    {
        var command = new CreatePatientCommand(dto.FirstName, dto.LastName, dto.DateOfBirth,
            dto.Gender, dto.Phone, dto.Email, dto.Address, dto.BloodGroup, dto.EmergencyContact);
        var patient = await _sender.Send(command);
        return CreatedAtAction(nameof(GetById), new { id = patient.Id }, patient);
    }

    [HttpGet("{id:guid}")]
    [EndpointSummary("Get a patient by ID")]
    [ProducesResponseType(typeof(PatientResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var patient = await _sender.Send(new GetPatientByIdQuery(id));
        return Ok(patient);
    }

    [HttpGet]
    [EndpointSummary("Get paginated patients list")]
    [ProducesResponseType(typeof(PagedResponse<PatientResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null, [FromQuery] string? sortBy = null, [FromQuery] bool descending = false)
    {
        var patients = await _sender.Send(new GetPatientsPagedQuery(page, pageSize, search, sortBy, descending));
        return Ok(patients);
    }
}