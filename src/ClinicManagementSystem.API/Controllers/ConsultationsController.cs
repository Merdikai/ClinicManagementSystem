using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagementSystem.API.Controllers;

[ApiController]
[Route("api/v1/consultations")]
public class ConsultationsController : ControllerBase
{
    private readonly IConsultationService _consultationService;

    public ConsultationsController(IConsultationService consultationService)
    {
        _consultationService = consultationService;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateConsultationDto dto)
    {
        // TODO: Get doctor ID from JWT token after auth is implemented
        var doctorId = Guid.NewGuid(); // Placeholder
        var consultation = await _consultationService.CreateAsync(dto, doctorId);
        return CreatedAtAction(nameof(Create), new { id = consultation.Id }, consultation);
    }
}