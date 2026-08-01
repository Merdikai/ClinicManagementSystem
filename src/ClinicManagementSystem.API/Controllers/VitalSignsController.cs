using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace ClinicManagementSystem.API.Controllers;

[ApiController]
[Route("api/v1/vitals")]
[Authorize(Roles = "Nurse")]
public class VitalSignsController : ControllerBase
{
    private readonly IVitalSignService _vitalSignService;

    public VitalSignsController(IVitalSignService vitalSignService)
    {
        _vitalSignService = vitalSignService;
    }

    [HttpPost]
public async Task<IActionResult> Record([FromBody] RecordVitalsDto dto)
{
    var nurseId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
    var vitals = await _vitalSignService.RecordAsync(dto, nurseId);
    return CreatedAtAction(nameof(Record), new { id = vitals.Id }, vitals);
}
}


    