using Asp.Versioning;
using AutoMapper;
using ClinicManagementSystem.Application.Consultations.Commands;
using ClinicManagementSystem.Application.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ClinicManagementSystem.API.Constants;
using Microsoft.AspNetCore.RateLimiting;

namespace ClinicManagementSystem.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/consultations")]
[Authorize(Roles = "Doctor")]
[EnableRateLimiting(RateLimitingConstants.StaffPolicy)]
public class ConsultationsController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IMapper _mapper;

    public ConsultationsController(ISender sender, IMapper mapper)
    {
        _sender = sender;
        _mapper = mapper;
    }

    [HttpPost]
    [EndpointSummary("Create a new consultation")]
    [ProducesResponseType(typeof(ConsultationResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateConsultationDto dto)
    {
        var doctorId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var command = new CreateConsultationCommand(dto.AppointmentId, dto.Symptoms, dto.Diagnosis, dto.ClinicalNotes, doctorId);
        var consultation = await _sender.Send(command);
        return Created(string.Empty, consultation);
    }
}