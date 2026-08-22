using Asp.Versioning;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.Prescriptions.Commands;
using ClinicManagementSystem.Infrastructure.Persistence.Context;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/prescriptions")]
[Authorize]
public class PrescriptionsController : ControllerBase
{
    private readonly ISender _sender;
    private readonly ClinicDbContext _context;

    public PrescriptionsController(ISender sender, ClinicDbContext context)
    {
        _sender = sender;
        _context = context;
    }

    [HttpGet]
    [EndpointSummary("Get all prescriptions")]
    public async Task<IActionResult> GetAllPrescriptions()
    {
        var prescriptions = await _context.Prescriptions
            .Include(p => p.Consultation)
                .ThenInclude(c => c.Appointment)
                    .ThenInclude(a => a.Patient)
            .Include(p => p.PrescriptionItems)
                .ThenInclude(pi => pi.Medicine)
            .OrderByDescending(p => p.IssuedAt)
            .Select(p => new
            {
                p.Id,
                p.ConsultationId,
                PatientName = p.Consultation != null && p.Consultation.Appointment != null && p.Consultation.Appointment.Patient != null
                    ? p.Consultation.Appointment.Patient.FirstName + " " + p.Consultation.Appointment.Patient.LastName
                    : "Patient",
                PatientMrn = p.Consultation != null && p.Consultation.Appointment != null && p.Consultation.Appointment.Patient != null
                    ? p.Consultation.Appointment.Patient.MedicalRecordNumber
                    : "",
                p.Notes,
                p.IssuedAt,
                Items = p.PrescriptionItems.Select(pi => new
                {
                    pi.Id,
                    pi.MedicineId,
                    MedicineName = pi.Medicine != null ? pi.Medicine.Name : "Medicine",
                    pi.Quantity,
                    pi.DosageInstructions,
                    pi.UnitPrice,
                    pi.TotalPrice
                })
            })
            .ToListAsync();

        return Ok(prescriptions);
    }

    [HttpGet("{prescriptionId:guid}")]
    [EndpointSummary("Get prescription by ID")]
    public async Task<IActionResult> GetById(Guid prescriptionId)
    {
        var p = await _context.Prescriptions
            .Include(p => p.Consultation)
                .ThenInclude(c => c.Appointment)
                    .ThenInclude(a => a.Patient)
            .Include(p => p.PrescriptionItems)
                .ThenInclude(pi => pi.Medicine)
            .FirstOrDefaultAsync(x => x.Id == prescriptionId);

        if (p == null) return NotFound(new ProblemDetails { Title = "Not Found", Detail = "Prescription not found", Status = 404 });

        return Ok(new
        {
            p.Id,
            p.ConsultationId,
            PatientName = p.Consultation != null && p.Consultation.Appointment != null && p.Consultation.Appointment.Patient != null
                ? p.Consultation.Appointment.Patient.FirstName + " " + p.Consultation.Appointment.Patient.LastName
                : "Patient",
            PatientMrn = p.Consultation != null && p.Consultation.Appointment != null && p.Consultation.Appointment.Patient != null
                ? p.Consultation.Appointment.Patient.MedicalRecordNumber
                : "",
            p.Notes,
            p.IssuedAt,
            Items = p.PrescriptionItems.Select(pi => new
            {
                pi.Id,
                pi.MedicineId,
                MedicineName = pi.Medicine != null ? pi.Medicine.Name : "Medicine",
                pi.Quantity,
                pi.DosageInstructions,
                pi.UnitPrice,
                pi.TotalPrice
            })
        });
    }

    [HttpPost("{prescriptionId:guid}/dispense")]
    [Authorize(Roles = "Pharmacist,Admin")]
    [EndpointSummary("Dispense a prescription and update medicine stock")]
    [ProducesResponseType(typeof(DispenseResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Dispense(Guid prescriptionId)
    {
        var result = await _sender.Send(new DispensePrescriptionCommand(prescriptionId));

        return result.Match<IActionResult>(
            onSuccess: dispense => Ok(dispense),
            onFailure: (error, errorCode) => errorCode switch
            {
                "prescription_not_found" => NotFound(new ProblemDetails { Title = "Not Found", Detail = error, Status = 404 }),
                _ => BadRequest(new ProblemDetails { Title = "Error", Detail = error, Status = 400 })
            }
        );
    }

    [HttpPost("{prescriptionId:guid}/invoice")]
    [Authorize(Roles = "Pharmacist,Accountant,Admin")]
    [EndpointSummary("Generate invoice from a prescription")]
    [ProducesResponseType(typeof(InvoiceResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GenerateInvoice(Guid prescriptionId)
    {
        var result = await _sender.Send(new GenerateInvoiceFromPrescriptionCommand(prescriptionId));

        return result.Match<IActionResult>(
            onSuccess: invoice => StatusCode(StatusCodes.Status201Created, invoice),
            onFailure: (error, errorCode) => errorCode switch
            {
                "prescription_not_found" => NotFound(new ProblemDetails { Title = "Not Found", Detail = error, Status = 404 }),
                _ => BadRequest(new ProblemDetails { Title = "Error", Detail = error, Status = 400 })
            }
        );
    }
}
