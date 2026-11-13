using Asp.Versioning;
using AutoMapper;
using ClinicManagementSystem.API.Authorization;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.Interfaces;
using ClinicManagementSystem.Application.Prescriptions.Commands;
using ClinicManagementSystem.Application.Prescriptions.DTOs;
using ClinicManagementSystem.Application.Prescriptions.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ClinicManagementSystem.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/prescriptions")]
[Authorize]
public class PrescriptionsController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IMapper _mapper;
    private readonly IClinicDbContext _context;
    private readonly IAuthorizationService _authorizationService;

    public PrescriptionsController(
        ISender sender,
        IMapper mapper,
        IClinicDbContext context,
        IAuthorizationService authorizationService)
    {
        _sender = sender;
        _mapper = mapper;
        _context = context;
        _authorizationService = authorizationService;
    }

    private async Task<Guid?> GetCurrentPatientIdAsync(CancellationToken ct = default)
    {
        var email = User.FindFirst(ClaimTypes.Email)?.Value
            ?? User.FindFirst("email")?.Value;

        if (string.IsNullOrWhiteSpace(email))
            return null;

        var pat = await _context.Patients
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Email == email, ct);

        return pat?.Id;
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Pharmacist,Doctor,Nurse,Patient")]
    [EndpointSummary("Get all prescriptions or filter by patient or status")]
    [ProducesResponseType(typeof(List<PrescriptionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllPrescriptions([FromQuery] Guid? patientId = null, [FromQuery] string? status = null, CancellationToken ct = default)
    {
        if (User.IsInRole("Patient") && !User.IsInRole("Admin"))
        {
            var myPatientId = await GetCurrentPatientIdAsync(ct);
            if (!myPatientId.HasValue) return Ok(new List<PrescriptionDto>());
            patientId = myPatientId.Value;
        }

        var result = await _sender.Send(new GetAllPrescriptionsQuery(patientId, status), ct);
        return Ok(result);
    }

    [HttpGet("{prescriptionId:guid}")]
    [Authorize(Roles = "Admin,Pharmacist,Doctor,Nurse,Patient")]
    [EndpointSummary("Get prescription by ID")]
    [ProducesResponseType(typeof(PrescriptionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetById(Guid prescriptionId, CancellationToken ct)
    {
        var result = await _sender.Send(new GetPrescriptionByIdQuery(prescriptionId), ct);
        if (result == null) return NotFound();

        var authResult = await _authorizationService.AuthorizeAsync(User, result, ClinicResourceOperationRequirement.Read);
        if (!authResult.Succeeded)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Title = "Forbidden",
                Detail = "Patients are only authorized to view their own prescriptions.",
                Status = StatusCodes.Status403Forbidden
            });
        }

        return Ok(result);
    }

    [HttpPost("dispense")]
    [Authorize(Roles = "Pharmacist")]
    [EndpointSummary("Dispense a prescription")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Dispense([FromBody] DispensePrescriptionDto dto, CancellationToken ct)
    {
        Guid? userId = GetCurrentUserId();
        var command = new DispensePrescriptionCommand(dto.PrescriptionId, userId, dto.VerificationNotes);
        var result = await _sender.Send(command, ct);

        return result.Match<IActionResult>(
            onSuccess: res => Ok(res),
            onFailure: (error, code) => BadRequest(new ProblemDetails
            {
                Title = "Dispensing Failed",
                Detail = error,
                Status = 400
            })
        );
    }

    [HttpPost("{id:guid}/dispense")]
    [Authorize(Roles = "Pharmacist")]
    [EndpointSummary("Dispense a prescription by ID with verification notes")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DispenseById(Guid id, [FromBody] DispensePrescriptionRequestDto? dto, CancellationToken ct)
    {
        Guid? userId = GetCurrentUserId();
        var command = new DispensePrescriptionCommand(id, userId, dto?.VerificationNotes);
        var result = await _sender.Send(command, ct);

        return result.Match<IActionResult>(
            onSuccess: res => Ok(res),
            onFailure: (error, code) => BadRequest(new ProblemDetails
            {
                Title = "Dispensing Failed",
                Detail = error,
                Status = 400
            })
        );
    }

    [HttpPost("{id:guid}/invoice")]
    [Authorize(Roles = "Pharmacist,Accountant,Receptionist")]
    [EndpointSummary("Generate billing invoice for a prescription")]
    [ProducesResponseType(typeof(InvoiceResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GenerateInvoice(Guid id, CancellationToken ct)
    {
        var command = new GeneratePrescriptionInvoiceCommand(id);
        var result = await _sender.Send(command, ct);

        return result.Match<IActionResult>(
            onSuccess: res => Ok(res),
            onFailure: (error, code) => BadRequest(new ProblemDetails
            {
                Title = "Invoice Generation Failed",
                Detail = error,
                Status = 400
            })
        );
    }

    [HttpGet("{id:guid}/label")]
    [Authorize(Roles = "Admin,Pharmacist")]
    [EndpointSummary("Generate patient medication dosage label for thermal printing")]
    [ProducesResponseType(typeof(List<PrescriptionLabelDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLabel(Guid id, [FromQuery] Guid? itemId = null, CancellationToken ct = default)
    {
        var labels = await _sender.Send(new GetPrescriptionLabelQuery(id, itemId), ct);
        return Ok(labels);
    }

    private Guid? GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out var uid) ? uid : null;
    }
}
