using AutoMapper;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ClinicManagementSystem.API.Controllers;

[ApiController]
[Route("api/v1/consultations")]
[Authorize(Roles = "Doctor")]
public class ConsultationsController : ControllerBase
{
    private readonly IConsultationService _consultationService;
    private readonly IMapper _mapper;

    public ConsultationsController(IConsultationService consultationService, IMapper mapper)
    {
        _consultationService = consultationService;
        _mapper = mapper;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateConsultationDto dto)
    {
        var doctorId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var consultation = await _consultationService.CreateAsync(dto, doctorId);
        return Created(string.Empty, consultation);
    }

}