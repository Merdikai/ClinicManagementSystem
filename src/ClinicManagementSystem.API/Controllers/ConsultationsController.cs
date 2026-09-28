using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Asp.Versioning;
using AutoMapper;
using ClinicManagementSystem.API.Constants;
using ClinicManagementSystem.Application.Consultations.Commands;
using ClinicManagementSystem.Application.Consultations.Queries;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClinicManagementSystem.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/consultations")]
[Authorize]
[EnableRateLimiting(RateLimitingConstants.StaffPolicy)]
public class ConsultationsController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUserService;

    public ConsultationsController(ISender sender, IMapper mapper, ICurrentUserService currentUserService)
    {
        _sender = sender;
        _mapper = mapper;
        _currentUserService = currentUserService;
    }

    [HttpPost]
    [Authorize(Roles = "Doctor,Admin")]
    [EndpointSummary("Create a new consultation with optional prescription")]
    [ProducesResponseType(typeof(ConsultationResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateConsultationDto dto)
    {
        var doctorId = _currentUserService.UserId ?? Guid.Empty;
        if (doctorId == Guid.Empty)
        {
            var doctorIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            Guid.TryParse(doctorIdStr, out doctorId);
        }

        if (doctorId == Guid.Empty)
            return Unauthorized();

        var isAdmin = User.IsInRole("Admin");

        var result = await _sender.Send(new CreateConsultationCommand(
            dto.AppointmentId,
            doctorId,
            dto.Symptoms,
            dto.Diagnosis,
            dto.Findings ?? string.Empty,
            dto.ClinicalNotes,
            dto.PrescriptionNotes,
            dto.PrescriptionItems,
            dto.SystolicBP,
            dto.DiastolicBP,
            dto.TemperatureC,
            dto.HeartRateBpm,
            dto.RespiratoryRate,
            dto.WeightKg,
            dto.HeightCm,
            dto.LabTestTemplateIds,
            dto.LabPriority,
            dto.LabClinicalNotes,
            dto.RadiologyOrders,
            dto.NursingProcedureOrders,
            isAdmin
        ));
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet]
    [Authorize(Roles = "Doctor,Nurse,Admin")]
    [EndpointSummary("Get paginated or filtered list of consultations")]
    [ProducesResponseType(typeof(List<ConsultationResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] Guid? doctorId = null, [FromQuery] Guid? patientId = null)
    {
        // When viewing a specific patient chart, allow doctors to view complete historical consultations across providers.
        // When browsing general consultation lists, isolate to the logged-in doctor's assigned cases.
        if (_currentUserService.IsDoctor && !_currentUserService.IsAdmin && !patientId.HasValue)
        {
            doctorId = _currentUserService.UserId;
        }

        var result = await _sender.Send(new GetConsultationsQuery(doctorId, patientId));
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Doctor,Nurse,Admin")]
    [EndpointSummary("Get consultation by ID")]
    [ProducesResponseType(typeof(ConsultationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _sender.Send(new GetConsultationByIdQuery(id));
        if (result == null)
            return NotFound(new ProblemDetails { Title = "Not Found", Detail = "Consultation not found", Status = 404 });

        return Ok(result);
    }

    [HttpGet("by-appointment/{appointmentId:guid}")]
    [Authorize(Roles = "Doctor,Nurse,Admin,Receptionist")]
    [EndpointSummary("Get consultation by appointment ID")]
    [ProducesResponseType(typeof(ConsultationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByAppointmentId(Guid appointmentId)
    {
        var result = await _sender.Send(new GetConsultationByAppointmentQuery(appointmentId));
        if (result == null)
            return NotFound(new ProblemDetails { Title = "Not Found", Detail = "No consultation found for this appointment", Status = 404 });

        return Ok(result);
    }
}
