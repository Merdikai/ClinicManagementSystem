using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagementSystem.API.Controllers;

[ApiController]
[Route("api/v1/vitals")]
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
        // TODO: Get nurse ID from JWT token after auth is implemented
        var nurseId = Guid.NewGuid(); // Placeholder
        var vitals = await _vitalSignService.RecordAsync(dto, nurseId);
        return CreatedAtAction(nameof(Record), new { id = vitals.Id }, vitals);
    }
}