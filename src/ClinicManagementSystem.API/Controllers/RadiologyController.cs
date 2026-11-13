using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Asp.Versioning;
using ClinicManagementSystem.API.Constants;
using ClinicManagementSystem.Application.Interfaces;
using ClinicManagementSystem.Application.Radiology.Commands;
using ClinicManagementSystem.Application.Radiology.DTOs;
using ClinicManagementSystem.Application.Radiology.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/radiology")]
[Authorize]
[EnableRateLimiting(RateLimitingConstants.StaffPolicy)]
public class RadiologyController : ControllerBase
{
    private readonly ISender _sender;
    private readonly ICurrentUserService _currentUserService;
    private readonly IClinicDbContext _context;

    public RadiologyController(ISender sender, ICurrentUserService currentUserService, IClinicDbContext context)
    {
        _sender = sender;
        _currentUserService = currentUserService;
        _context = context;
    }

    [HttpGet("orders")]
    [Authorize(Roles = "Doctor,Radiologist,Admin,Nurse,Patient,Accountant")]
    [EndpointSummary("Get paginated or filtered list of radiology imaging orders")]
    [ProducesResponseType(typeof(List<RadiologyOrderDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOrders(
        [FromQuery] Guid? doctorId = null,
        [FromQuery] Guid? patientId = null,
        [FromQuery] string? modality = null,
        [FromQuery] string? status = null,
        [FromQuery] string? priority = null,
        [FromQuery] string? radiologistName = null,
        CancellationToken ct = default)
    {
        // Patient Isolation Enforcement: Patients can ONLY view their own imaging orders
        if (User.IsInRole("Patient") && !_currentUserService.IsAdmin)
        {
            var email = _currentUserService.Email
                ?? User.FindFirst(ClaimTypes.Email)?.Value
                ?? User.FindFirst("email")?.Value;

            if (string.IsNullOrWhiteSpace(email))
                return Ok(new List<RadiologyOrderDto>());

            var currentPatient = await _context.Patients
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Email == email, ct);

            if (currentPatient is null)
                return Ok(new List<RadiologyOrderDto>());

            patientId = currentPatient.Id;
        }

        var result = await _sender.Send(new GetRadiologyOrdersQuery(doctorId, patientId, modality, status, priority, radiologistName), ct);
        return Ok(result);
    }

    [HttpGet("orders/{id:guid}")]
    [Authorize(Roles = "Doctor,Radiologist,Admin,Nurse,Patient,Accountant")]
    [EndpointSummary("Get detailed diagnostic imaging order and report by ID")]
    [ProducesResponseType(typeof(RadiologyOrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrderById(Guid id, CancellationToken ct)
    {
        var result = await _sender.Send(new GetRadiologyOrderByIdQuery(id), ct);
        if (result == null)
            return NotFound(new ProblemDetails { Title = "Not Found", Detail = $"Radiology order '{id}' not found.", Status = 404 });

        // Patient Isolation Enforcement: Patient cannot access another patient's radiology order
        if (User.IsInRole("Patient") && !_currentUserService.IsAdmin)
        {
            var email = _currentUserService.Email
                ?? User.FindFirst(ClaimTypes.Email)?.Value
                ?? User.FindFirst("email")?.Value;

            var currentPatient = !string.IsNullOrWhiteSpace(email)
                ? await _context.Patients.AsNoTracking().FirstOrDefaultAsync(p => p.Email == email, ct)
                : null;

            if (currentPatient is null || result.PatientId != currentPatient.Id)
            {
                return Forbid();
            }
        }

        return Ok(result);
    }

    [HttpGet("metrics")]
    [Authorize(Roles = "Doctor,Radiologist,Admin,Nurse,Accountant")]
    [EndpointSummary("Get clinical imaging statistics and review counts")]
    [ProducesResponseType(typeof(RadiologyMetricsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMetrics([FromQuery] Guid? doctorId = null, [FromQuery] string? radiologistName = null)
    {
        if (_currentUserService.IsDoctor && !_currentUserService.IsAdmin && !_currentUserService.IsRadiologist && doctorId == null)
        {
            doctorId = _currentUserService.UserId;
        }

        var result = await _sender.Send(new GetRadiologyMetricsQuery(doctorId, radiologistName));
        return Ok(result);
    }

    [HttpPost("orders")]
    [Authorize(Roles = "Doctor,Radiologist,Admin")]
    [EndpointSummary("Order a diagnostic imaging study (X-Ray, US, CT, MRI, ECG)")]
    [ProducesResponseType(typeof(RadiologyOrderDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateOrder([FromBody] CreateRadiologyOrderDto dto)
    {
        var doctorId = _currentUserService.UserId ?? Guid.Empty;
        if (doctorId == Guid.Empty)
        {
            var doctorIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            Guid.TryParse(doctorIdStr, out doctorId);
        }

        if (doctorId == Guid.Empty)
        {
            return Unauthorized(new ProblemDetails { Title = "Unauthorized", Detail = "Authenticated clinician identity is required.", Status = 401 });
        }

        var result = await _sender.Send(new CreateRadiologyOrderCommand(
            dto.PatientId,
            doctorId,
            dto.AppointmentId,
            dto.Modality,
            dto.ProcedureName,
            dto.BodyPart,
            dto.ClinicalIndication,
            dto.Priority,
            dto.Price,
            dto.RoomSuite
        ));

        return CreatedAtAction(nameof(GetOrderById), new { id = result.Id }, result);
    }

    [HttpPatch("orders/{id:guid}/schedule")]
    [Authorize(Roles = "Radiologist,Doctor,Admin")]
    [EndpointSummary("Assign imaging room/suite and scheduled timeslot for study")]
    [ProducesResponseType(typeof(RadiologyOrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ScheduleOrder(Guid id, [FromBody] ScheduleRadiologyOrderDto dto)
    {
        var result = await _sender.Send(new ScheduleRadiologyOrderCommand(
            id,
            dto.RoomSuite,
            dto.EquipmentModel,
            dto.ScheduledAt
        ));
        return Ok(result);
    }

    [HttpPost("orders/{id:guid}/report")]
    [Authorize(Roles = "Radiologist,Doctor,Admin")]
    [EndpointSummary("Radiologist publishes diagnostic findings, critical alerts, contrast and radiation dose")]
    [ProducesResponseType(typeof(RadiologyOrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReportOrder(Guid id, [FromBody] ReportRadiologyOrderDto dto)
    {
        var technicianName = User.FindFirst("fullName")?.Value
            ?? (User.FindFirst(ClaimTypes.GivenName)?.Value != null && User.FindFirst(ClaimTypes.Surname)?.Value != null
                ? $"{User.FindFirst(ClaimTypes.GivenName)?.Value} {User.FindFirst(ClaimTypes.Surname)?.Value}".Trim()
                : null)
            ?? User.FindFirst(ClaimTypes.Name)?.Value
            ?? _currentUserService.UserName
            ?? "Attending Radiologist";

        var result = await _sender.Send(new ReportRadiologyOrderCommand(
            id,
            technicianName,
            dto.Findings,
            dto.Impression,
            dto.ImageUrl,
            dto.RadiologistSignature,
            dto.IsCritical,
            dto.CriticalAlertNotes,
            dto.SecondaryAttachmentUrls,
            dto.RadiationDoseDlp,
            dto.RadiationDoseDap,
            dto.ExposureParameters,
            dto.ContrastUsed,
            dto.ContrastAgentName,
            dto.ContrastVolumeMl,
            dto.ContrastLotNumber,
            dto.ContrastRoute
        ));

        return Ok(result);
    }

    [HttpPatch("orders/{id:guid}/critical-ack")]
    [Authorize(Roles = "Doctor,Radiologist,Admin")]
    [EndpointSummary("Attending Doctor or Radiologist acknowledges critical / emergency diagnostic imaging finding")]
    [ProducesResponseType(typeof(RadiologyOrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AcknowledgeCriticalAlert(Guid id, [FromBody] AcknowledgeCriticalAlertDto dto)
    {
        var doctorId = _currentUserService.UserId ?? Guid.Empty;
        if (doctorId == Guid.Empty)
        {
            var doctorIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            Guid.TryParse(doctorIdStr, out doctorId);
        }

        var result = await _sender.Send(new AcknowledgeCriticalAlertCommand(id, doctorId, dto?.AcknowledgmentNotes));
        return Ok(result);
    }

    [HttpPost("orders/{id:guid}/doctor-signoff")]
    [Authorize(Roles = "Doctor,Admin")]
    [EndpointSummary("Attending Doctor signs off and acknowledges reported imaging results")]
    [ProducesResponseType(typeof(RadiologyOrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DoctorSignOff(Guid id, [FromBody] DoctorSignOffRadiologyDto dto)
    {
        var doctorId = _currentUserService.UserId ?? Guid.Empty;
        if (doctorId == Guid.Empty)
        {
            var doctorIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            Guid.TryParse(doctorIdStr, out doctorId);
        }

        var isAdmin = _currentUserService.IsAdmin || User.IsInRole("Admin");
        var result = await _sender.Send(new DoctorSignOffRadiologyCommand(id, doctorId, dto?.Notes, isAdmin));
        return Ok(result);
    }

    [HttpGet("safety-logs")]
    [Authorize(Roles = "Radiologist,Doctor,Admin,Nurse")]
    [EndpointSummary("Get radiation dosimetry compliance and contrast media administration records")]
    [ProducesResponseType(typeof(List<RadiologySafetyLogDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSafetyLogs([FromQuery] string? modality = null)
    {
        var result = await _sender.Send(new GetRadiologySafetyLogsQuery(modality));
        return Ok(result);
    }

    [HttpPost("safety-logs")]
    [Authorize(Roles = "Radiologist,Admin")]
    [EndpointSummary("Create or record a radiation dosimetry or contrast media safety log")]
    [ProducesResponseType(typeof(RadiologySafetyLogDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateSafetyLog([FromBody] CreateRadiologySafetyEntryDto dto)
    {
        var technicianName = User.FindFirst("fullName")?.Value
            ?? (User.FindFirst(ClaimTypes.GivenName)?.Value != null && User.FindFirst(ClaimTypes.Surname)?.Value != null
                ? $"{User.FindFirst(ClaimTypes.GivenName)?.Value} {User.FindFirst(ClaimTypes.Surname)?.Value}".Trim()
                : null)
            ?? User.FindFirst(ClaimTypes.Name)?.Value
            ?? _currentUserService.UserName
            ?? "Attending Radiologist";

        var result = await _sender.Send(new CreateRadiologySafetyEntryCommand(
            dto.AccessionNumber,
            dto.PatientName,
            dto.PatientId,
            dto.Modality,
            dto.ProcedureName,
            dto.PerformedByTechnicianName ?? technicianName,
            dto.RadiationDoseDlp,
            dto.RadiationDoseDap,
            dto.ExposureParameters,
            dto.ContrastUsed,
            dto.ContrastAgentName,
            dto.ContrastVolumeMl,
            dto.ContrastLotNumber,
            dto.ContrastRoute
        ));
        return Ok(result);
    }

    [HttpPatch("orders/{id:guid}/bill")]
    [Authorize(Roles = "Accountant,Admin,Radiologist")]
    [EndpointSummary("Mark a radiology order as billed")]
    [ProducesResponseType(typeof(RadiologyOrderDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> BillOrder(Guid id)
    {
        var result = await _sender.Send(new BillRadiologyOrderCommand(id));
        return Ok(result);
    }

    [HttpPatch("orders/{id:guid}/status")]
    [Authorize(Roles = "Radiologist,Doctor,Nurse,Admin")]
    [EndpointSummary("Update imaging procedure status (e.g. Scheduled, InProgress / In Scan, Completed)")]
    [ProducesResponseType(typeof(RadiologyOrderDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateRadiologyStatusDto dto)
    {
        var result = await _sender.Send(new UpdateRadiologyOrderStatusCommand(id, dto.Status, dto.Notes));
        return Ok(result);
    }
}
