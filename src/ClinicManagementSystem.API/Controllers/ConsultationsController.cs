using Asp.Versioning;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Domain.Entities;
using ClinicManagementSystem.Domain.Enums;
using ClinicManagementSystem.Infrastructure.Persistence.Context;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using ClinicManagementSystem.API.Constants;
using Microsoft.AspNetCore.RateLimiting;

namespace ClinicManagementSystem.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/consultations")]
[Authorize]
[EnableRateLimiting(RateLimitingConstants.StaffPolicy)]
public class ConsultationsController : ControllerBase
{
    private readonly ClinicDbContext _context;

    public ConsultationsController(ClinicDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    [Authorize(Roles = "Doctor,Admin")]
    [EndpointSummary("Create a new consultation with optional prescription")]
    [ProducesResponseType(typeof(ConsultationResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateConsultationDto dto)
    {
        var doctorIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        Guid doctorId = Guid.Empty;
        if (!string.IsNullOrEmpty(doctorIdStr))
        {
            Guid.TryParse(doctorIdStr, out doctorId);
        }

        var appointment = await _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .FirstOrDefaultAsync(a => a.Id == dto.AppointmentId);

        if (appointment == null)
        {
            return NotFound(new ProblemDetails { Title = "Not Found", Detail = $"Appointment {dto.AppointmentId} not found", Status = 404 });
        }

        if (doctorId == Guid.Empty)
        {
            doctorId = appointment.DoctorId;
        }

        var consultation = new Consultation
        {
            AppointmentId = dto.AppointmentId,
            DoctorId = doctorId,
            Symptoms = dto.Symptoms ?? string.Empty,
            Diagnosis = dto.Diagnosis ?? string.Empty,
            ClinicalNotes = dto.ClinicalNotes ?? string.Empty,
            ConsultedAt = DateTime.UtcNow
        };

        await _context.Consultations.AddAsync(consultation);

        // Mark appointment completed
        appointment.Status = AppointmentStatus.Completed;

        // Process prescription if items provided
        if (dto.PrescriptionItems != null && dto.PrescriptionItems.Count > 0)
        {
            var prescription = new Prescription
            {
                Consultation = consultation,
                Notes = dto.PrescriptionNotes ?? "Standard prescription issued by doctor",
                IssuedAt = DateTime.UtcNow
            };

            foreach (var itemDto in dto.PrescriptionItems)
            {
                var medicine = await _context.Medicines.FindAsync(itemDto.MedicineId);
                var unitPrice = medicine?.UnitPrice ?? 10.0m;

                prescription.PrescriptionItems.Add(new PrescriptionItem
                {
                    MedicineId = itemDto.MedicineId,
                    Quantity = itemDto.Quantity > 0 ? itemDto.Quantity : 1,
                    UnitPrice = unitPrice,
                    DosageInstructions = itemDto.DosageInstructions ?? "As directed by physician"
                });
            }

            await _context.Prescriptions.AddAsync(prescription);
        }

        await _context.SaveChangesAsync();

        var doctorName = appointment.Doctor != null
            ? $"{appointment.Doctor.FirstName} {appointment.Doctor.LastName}"
            : "Doctor";

        var response = new ConsultationResponseDto
        {
            Id = consultation.Id,
            AppointmentId = consultation.AppointmentId,
            DoctorName = doctorName,
            Symptoms = consultation.Symptoms,
            Diagnosis = consultation.Diagnosis,
            ClinicalNotes = consultation.ClinicalNotes,
            ConsultedAt = consultation.ConsultedAt
        };

        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpGet("appointment/{appointmentId:guid}")]
    [EndpointSummary("Get consultation details by appointment ID")]
    [ProducesResponseType(typeof(ConsultationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByAppointment(Guid appointmentId)
    {
        var consultation = await _context.Consultations
            .Include(c => c.Doctor)
            .Include(c => c.Prescription)
                .ThenInclude(p => p.PrescriptionItems)
                    .ThenInclude(pi => pi.Medicine)
            .FirstOrDefaultAsync(c => c.AppointmentId == appointmentId);

        if (consultation == null)
            return NotFound(new ProblemDetails { Title = "Not Found", Detail = $"Consultation for appointment {appointmentId} not found", Status = 404 });

        var doctorName = consultation.Doctor != null
            ? $"{consultation.Doctor.FirstName} {consultation.Doctor.LastName}"
            : "Doctor";

        var response = new ConsultationResponseDto
        {
            Id = consultation.Id,
            AppointmentId = consultation.AppointmentId,
            DoctorName = doctorName,
            Symptoms = consultation.Symptoms,
            Diagnosis = consultation.Diagnosis,
            ClinicalNotes = consultation.ClinicalNotes,
            ConsultedAt = consultation.ConsultedAt
        };

        return Ok(response);
    }
}
