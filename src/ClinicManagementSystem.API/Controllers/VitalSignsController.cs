using AutoMapper;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ClinicManagementSystem.API.Controllers;

[ApiController]
[Route("api/v1/vitals")]
[Authorize(Roles = "Nurse")]
public class VitalSignsController : ControllerBase
{
    private readonly IVitalSignService _vitalSignService;
    private readonly IMapper _mapper;

    public VitalSignsController(IVitalSignService vitalSignService, IMapper mapper)
    {
        _vitalSignService = vitalSignService;
        _mapper = mapper;
    }

    [HttpPost]
    public async Task<IActionResult> Record([FromBody] RecordVitalsDto dto)
    {
        var nurseId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var vitals = await _vitalSignService.RecordAsync(dto, nurseId);
        return Created(string.Empty, vitals);
    }
}


    