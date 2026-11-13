using Asp.Versioning;
using AutoMapper;
using ClinicManagementSystem.API.Authorization;
using ClinicManagementSystem.API.Constants;
using ClinicManagementSystem.Application.Appointments.Commands;
using ClinicManagementSystem.Application.Appointments.Queries;
using ClinicManagementSystem.Application.Common;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.Interfaces;
using ClinicManagementSystem.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ClinicManagementSystem.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/appointments")]
[Tags("Appointments")]
[Authorize]
[EnableRateLimiting(RateLimitingConstants.PatientPolicy)]
public class AppointmentsController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUserService;
    private readonly IClinicDbContext _context;
    private readonly IAuthorizationService _authorizationService;

    public AppointmentsController(
        ISender sender,
        IMapper mapper,
        ICurrentUserService currentUserService,
        IClinicDbContext context,
        IAuthorizationService authorizationService)
    {
        _sender = sender;
        _mapper = mapper;
        _currentUserService = currentUserService;
        _context = context;
        _authorizationService = authorizationService;
    }

    private async Task<Guid?> GetCurrentPatientIdAsync(CancellationToken ct = default)
    {
        var email = User.FindFirst(ClaimTypes.Email)?.Value
            ?? User.FindFirst("email")?.Value
            ?? _currentUserService.Email;

        if (string.IsNullOrWhiteSpace(email))
            return null;

        var pat = await _context.Patients
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Email == email, ct);

        return pat?.Id;
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Receptionist,Nurse,Patient")]
    [EndpointSummary("Create a new appointment")]
    [ProducesResponseType(typeof(AppointmentResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateAppointmentDto dto, CancellationToken ct)
    {
        if (User.IsInRole("Patient"))
        {
            var myPatientId = await GetCurrentPatientIdAsync(ct);
            if (myPatientId == null)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
                {
                    Title = "Patient Record Not Linked",
                    Detail = "Your authenticated account does not have a linked patient record.",
                    Status = StatusCodes.Status403Forbidden
                });
            }
            dto = dto with { PatientId = myPatientId.Value };
        }

        var command = new CreateAppointmentCommand(dto.PatientId, dto.DoctorId, dto.ScheduledDateTime, dto.DurationMinutes, dto.ReasonForVisit);
        var result = await _sender.Send(command, ct);

        return result.Match<IActionResult>(
            onSuccess: appointment => CreatedAtAction(nameof(GetById), new { id = appointment.Id }, appointment),
            onFailure: (error, errorCode) => errorCode switch
            {
                "slot_unavailable" => Conflict(new ProblemDetails
                {
                    Title = "Slot Unavailable",
                    Detail = error,
                    Status = 409
                }),
                _ => BadRequest(new ProblemDetails
                {
                    Title = "Error",
                    Detail = error,
                    Status = 400
                })
            }
        );
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Receptionist,Doctor,Nurse,Patient")]
    [EndpointSummary("Get paginated appointments")]
    [ProducesResponseType(typeof(PagedResponse<AppointmentResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] Guid? patientId = null,
        [FromQuery] AppointmentStatus? status = null,
        CancellationToken ct = default)
    {
        if (User.IsInRole("Patient") && !User.IsInRole("Admin"))
        {
            var myPatientId = await GetCurrentPatientIdAsync(ct);
            if (!myPatientId.HasValue)
            {
                return Ok(new PagedResponse<AppointmentResponseDto>(
                    new List<AppointmentResponseDto>(), page, pageSize, 0, 0, false, false));
            }
            patientId = myPatientId.Value;
        }

        var result = await _sender.Send(new GetAppointmentsPagedQuery(page, pageSize, startDate, endDate, patientId, status), ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Admin,Receptionist,Doctor,Nurse,Patient")]
    [EndpointSummary("Get appointment by ID")]
    [ProducesResponseType(typeof(AppointmentResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var appointment = await _sender.Send(new GetAppointmentByIdQuery(id), ct);
        if (appointment is null)
            return NotFound(new ProblemDetails { Title = "Not Found", Detail = $"Appointment '{id}' was not found.", Status = 404 });

        var authResult = await _authorizationService.AuthorizeAsync(User, appointment, AppointmentOperationRequirement.Read);
        if (!authResult.Succeeded)
            return Forbid();

        return Ok(appointment);
    }

    [HttpGet("my-appointments")]
    [Authorize(Roles = "Doctor,Patient,Admin,Nurse,Receptionist")]
    [EndpointSummary("Get appointments for the currently logged-in doctor or patient")]
    [ProducesResponseType(typeof(IEnumerable<AppointmentResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyAppointments([FromQuery] DateTime? date = null, [FromQuery] Guid? patientId = null, CancellationToken ct = default)
    {
        if (User.IsInRole("Patient"))
        {
            var myPatientId = await GetCurrentPatientIdAsync(ct);
            if (myPatientId.HasValue)
            {
                var paged = await _sender.Send(new GetAppointmentsPagedQuery(1, 100, null, null, myPatientId.Value, null), ct);
                return Ok(paged.Items);
            }
            return Ok(new List<AppointmentResponseDto>());
        }

        if (patientId.HasValue && patientId.Value != Guid.Empty)
        {
            var paged = await _sender.Send(new GetAppointmentsPagedQuery(1, 100, null, null, patientId.Value, null), ct);
            return Ok(paged.Items);
        }

        if (_currentUserService.UserId == null)
            return Unauthorized(new ProblemDetails { Title = "Unauthorized", Detail = "User identity not found in token", Status = 401 });

        var appointments = await _sender.Send(new GetAppointmentsByDoctorQuery(_currentUserService.UserId.Value, date), ct);
        return Ok(appointments);
    }

    [HttpGet("doctor/{doctorId:guid}")]
    [Authorize(Roles = "Admin,Doctor,Nurse,Receptionist")]
    [EndpointSummary("Get appointments for a specific doctor")]
    [ProducesResponseType(typeof(IEnumerable<AppointmentResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByDoctor(Guid doctorId, [FromQuery] DateTime? date = null, CancellationToken ct = default)
    {
        var appointments = await _sender.Send(new GetAppointmentsByDoctorQuery(doctorId, date), ct);
        return Ok(appointments);
    }

    [HttpPatch("{id:guid}/approve")]
    [Authorize(Roles = "Doctor,Admin")]
    [EndpointSummary("Approve a pending appointment")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Approve(Guid id, CancellationToken ct)
    {
        var entity = await _context.Appointments.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, ct);
        if (entity is null)
            return NotFound(new ProblemDetails { Title = "Not Found", Detail = "Appointment not found", Status = 404 });

        var authResult = await _authorizationService.AuthorizeAsync(User, entity, AppointmentOperationRequirement.Modify);
        if (!authResult.Succeeded)
            return Forbid();

        var doctorId = _currentUserService.UserId;
        var isAdmin = _currentUserService.IsAdmin || User.IsInRole("Admin");
        var result = await _sender.Send(new ApproveAppointmentCommand(id, doctorId, isAdmin), ct);
        return result.Match<IActionResult>(
            onSuccess: _ => Ok(new { success = true, message = "Appointment approved successfully." }),
            onFailure: (err, code) => code switch
            {
                "unauthorized" => StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails { Title = "Forbidden", Detail = err, Status = 403 }),
                _ => BadRequest(new ProblemDetails { Title = "Approval Failed", Detail = err, Status = 400 })
            }
        );
    }

    [HttpPatch("{id:guid}/checkin")]
    [Authorize(Roles = "Receptionist,Nurse,Admin")]
    [EndpointSummary("Check-in an appointment with optional token and visit type")]
    [ProducesResponseType(typeof(AppointmentResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CheckIn(Guid id, [FromBody] CheckInAppointmentDto? dto = null, CancellationToken ct = default)
    {
        var result = await _sender.Send(new CheckInAppointmentCommand(id, dto?.TokenNumber, dto?.VisitType), ct);
        return result.Match<IActionResult>(
            onSuccess: appt => Ok(appt),
            onFailure: (err, _) => BadRequest(new ProblemDetails { Title = "Check-In Failed", Detail = err, Status = 400 })
        );
    }

    [HttpPatch("{id:guid}/reschedule")]
    [Authorize(Roles = "Receptionist,Admin,Doctor,Nurse")]
    [EndpointSummary("Reschedule an appointment to a new date/time")]
    [ProducesResponseType(typeof(AppointmentResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Reschedule(Guid id, [FromBody] RescheduleAppointmentDto dto, CancellationToken ct)
    {
        var entity = await _context.Appointments.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, ct);
        if (entity is null)
            return NotFound(new ProblemDetails { Title = "Not Found", Detail = "Appointment not found", Status = 404 });

        var authResult = await _authorizationService.AuthorizeAsync(User, entity, AppointmentOperationRequirement.Modify);
        if (!authResult.Succeeded)
            return Forbid();

        var result = await _sender.Send(new RescheduleAppointmentCommand(id, dto.NewScheduledDateTime, dto.Reason), ct);
        return result.Match<IActionResult>(
            onSuccess: appt => Ok(appt),
            onFailure: (err, _) => BadRequest(new ProblemDetails { Title = "Reschedule Failed", Detail = err, Status = 400 })
        );
    }

    [HttpPatch("{id:guid}/no-show")]
    [Authorize(Roles = "Receptionist,Admin,Doctor,Nurse")]
    [EndpointSummary("Mark appointment as No-Show")]
    [ProducesResponseType(typeof(AppointmentResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> MarkNoShow(Guid id, [FromBody] MarkNoShowDto? dto = null, CancellationToken ct = default)
    {
        var result = await _sender.Send(new MarkNoShowAppointmentCommand(id, dto?.Notes), ct);
        return result.Match<IActionResult>(
            onSuccess: appt => Ok(appt),
            onFailure: (err, _) => BadRequest(new ProblemDetails { Title = "Mark No-Show Failed", Detail = err, Status = 400 })
        );
    }

    [HttpPatch("{id:guid}/cancel")]
    [Authorize(Roles = "Receptionist,Admin,Doctor,Nurse,Patient")]
    [EndpointSummary("Cancel an appointment")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        var entity = await _context.Appointments.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, ct);
        if (entity is null)
            return NotFound(new ProblemDetails { Title = "Not Found", Detail = "Appointment not found", Status = 404 });

        var authResult = await _authorizationService.AuthorizeAsync(User, entity, AppointmentOperationRequirement.Cancel);
        if (!authResult.Succeeded)
            return Forbid();

        await _sender.Send(new CancelAppointmentCommand(id), ct);
        return NoContent();
    }

    [HttpPatch("{id:guid}/triage")]
    [Authorize(Roles = "Nurse,Doctor,Admin")]
    [EndpointSummary("Update triage assessment, notes and room assignment")]
    [ProducesResponseType(typeof(AppointmentResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateTriage(Guid id, [FromBody] UpdateAppointmentTriageDto dto, CancellationToken ct)
    {
        var command = new UpdateAppointmentTriageCommand(id, dto.TriageUrgency, dto.RoomAssignment, dto.TriageNotes, dto.Status);
        var result = await _sender.Send(command, ct);
        return result.Match<IActionResult>(
            onSuccess: appt => Ok(appt),
            onFailure: (err, _) => BadRequest(new ProblemDetails { Title = "Triage Update Failed", Detail = err, Status = 400 })
        );
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = "Admin,Doctor,Nurse,Receptionist")]
    [EndpointSummary("Update appointment clinical queue status")]
    [ProducesResponseType(typeof(AppointmentResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateAppointmentStatusDto dto, CancellationToken ct)
    {
        var command = new UpdateAppointmentStatusCommand(id, dto.Status, dto.RoomAssignment);
        var result = await _sender.Send(command, ct);
        return result.Match<IActionResult>(
            onSuccess: appt => Ok(appt),
            onFailure: (err, _) => BadRequest(new ProblemDetails { Title = "Status Update Failed", Detail = err, Status = 400 })
        );
    }

    [HttpGet("deleted")]
    [Authorize(Roles = "Admin")]
    [EndpointSummary("List soft-deleted appointments for Admin recovery")]
    [ProducesResponseType(typeof(List<AppointmentResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDeletedAppointments(CancellationToken ct = default)
    {
        var deleted = await _sender.Send(new GetDeletedAppointmentsQuery(), ct);
        return Ok(deleted);
    }

    [HttpPost("{id:guid}/restore")]
    [Authorize(Roles = "Admin")]
    [EndpointSummary("Restore a soft-deleted appointment (Admin only)")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RestoreAppointment(Guid id, CancellationToken ct = default)
    {
        var result = await _sender.Send(new RestoreAppointmentCommand(id), ct);
        return result.Match<IActionResult>(
            onSuccess: _ => NoContent(),
            onFailure: (error, _) => NotFound(new ProblemDetails { Title = "Not Found", Detail = error, Status = 404 })
        );
    }
}
