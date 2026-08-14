using Asp.Versioning;
using AutoMapper;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.VitalSigns.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ClinicManagementSystem.API.Constants;
using Microsoft.AspNetCore.RateLimiting;

namespace ClinicManagementSystem.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/vitals")]
[Authorize(Roles = "Nurse")]
[EnableRateLimiting(RateLimitingConstants.StaffPolicy)]
public class VitalSignsController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IMapper _mapper;

    public VitalSignsController(ISender sender, IMapper mapper)
    {
        _sender = sender;
        _mapper = mapper;
    }

    [HttpPost]
    [EndpointSummary("Record vital signs")]
    [ProducesResponseType(typeof(VitalSignResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Record([FromBody] RecordVitalsDto dto)
    {
        var nurseId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var command = new RecordVitalsCommand(dto.AppointmentId, dto.SystolicBP, dto.DiastolicBP,
            dto.TemperatureC, dto.HeartRateBpm, dto.RespiratoryRate, dto.WeightKg, dto.HeightCm, nurseId);
        var vitals = await _sender.Send(command);
        return Created(string.Empty, vitals);
    }
}