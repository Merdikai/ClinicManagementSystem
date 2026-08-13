using AutoMapper;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.VitalSigns.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ClinicManagementSystem.API.Controllers;

[ApiController]
[Route("api/v1/vitals")]
[Authorize(Roles = "Nurse")]
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
    public async Task<IActionResult> Record([FromBody] RecordVitalsDto dto)
    {
        var nurseId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var command = new RecordVitalsCommand(dto.AppointmentId, dto.SystolicBP, dto.DiastolicBP,
            dto.TemperatureC, dto.HeartRateBpm, dto.RespiratoryRate, dto.WeightKg, dto.HeightCm, nurseId);
        var vitals = await _sender.Send(command);
        return Created(string.Empty, vitals);
    }
}