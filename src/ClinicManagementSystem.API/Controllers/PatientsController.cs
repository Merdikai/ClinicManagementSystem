using AutoMapper;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.Patients.Commands;
using ClinicManagementSystem.Application.Patients.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagementSystem.API.Controllers;

[ApiController]
[Route("api/v1/patients")]
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
    public async Task<IActionResult> Create([FromBody] CreatePatientDto dto)
    {
        var command = new CreatePatientCommand(dto.FirstName, dto.LastName, dto.DateOfBirth,
            dto.Gender, dto.Phone, dto.Email, dto.Address, dto.BloodGroup, dto.EmergencyContact);
        var patient = await _sender.Send(command);
        return CreatedAtAction(nameof(GetById), new { id = patient.Id }, patient);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var patient = await _sender.Send(new GetPatientByIdQuery(id));
        return Ok(patient);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null)
    {
        var patients = await _sender.Send(new GetPatientsPagedQuery(page, pageSize, search));
        return Ok(patients);
    }
}