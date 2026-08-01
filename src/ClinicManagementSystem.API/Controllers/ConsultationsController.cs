using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace ClinicManagementSystem.API.Controllers;

[ApiController]
[Route("api/v1/consultations")]
[Authorize(Roles = "Doctor")]
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
    var doctorId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
    var consultation = await _consultationService.CreateAsync(dto, doctorId);
    return CreatedAtAction(nameof(Create), new { id = consultation.Id }, consultation);
}

}