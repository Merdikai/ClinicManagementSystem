using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Asp.Versioning;
using AutoMapper;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.Interfaces;
using ClinicManagementSystem.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/nursing-procedures")]
[Tags("Nursing Procedures")]
[Authorize]
public class NursingProceduresController : ControllerBase
{
    private readonly IClinicDbContext _context;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUserService;

    public NursingProceduresController(
        IClinicDbContext context,
        IMapper mapper,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _mapper = mapper;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    [EndpointSummary("Get list of nursing procedures with optional filters")]
    [ProducesResponseType(typeof(IEnumerable<NursingProcedureResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? patientId = null,
        [FromQuery] Guid? appointmentId = null,
        [FromQuery] DateTime? date = null,
        [FromQuery] string? status = null)
    {
        var query = _context.NursingProcedures
            .Include(p => p.Patient)
            .Include(p => p.PerformedByUser)
            .Include(p => p.OrderedByDoctor)
            .AsNoTracking()
            .AsQueryable();

        if (patientId.HasValue && patientId.Value != Guid.Empty)
        {
            query = query.Where(p => p.PatientId == patientId.Value);
        }

        if (appointmentId.HasValue && appointmentId.Value != Guid.Empty)
        {
            query = query.Where(p => p.AppointmentId == appointmentId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(p => p.Status == status);
        }

        if (date.HasValue)
        {
            var start = date.Value.Date;
            var end = start.AddDays(1);
            query = query.Where(p => (p.PerformedAt.HasValue && p.PerformedAt.Value >= start && p.PerformedAt.Value < end) || (p.OrderedAt >= start && p.OrderedAt < end));
        }

        var items = await query
            .OrderByDescending(p => p.OrderedAt)
            .ToListAsync();

        var dtos = _mapper.Map<IEnumerable<NursingProcedureResponseDto>>(items);
        return Ok(dtos);
    }

    [HttpGet("{id:guid}")]
    [EndpointSummary("Get a nursing procedure by ID")]
    [ProducesResponseType(typeof(NursingProcedureResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var item = await _context.NursingProcedures
            .Include(p => p.Patient)
            .Include(p => p.PerformedByUser)
            .Include(p => p.OrderedByDoctor)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);

        if (item == null)
        {
            return NotFound(new ProblemDetails { Title = "Not Found", Detail = $"Nursing procedure {id} not found.", Status = 404 });
        }

        return Ok(_mapper.Map<NursingProcedureResponseDto>(item));
    }

    [HttpPost]
    [Authorize(Roles = "Nurse,Doctor,Admin")]
    [EndpointSummary("Record a direct nursing procedure performed immediately")]
    [ProducesResponseType(typeof(NursingProcedureResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateNursingProcedureDto dto)
    {
        if (dto.PatientId == Guid.Empty)
        {
            return BadRequest(new ProblemDetails { Title = "Invalid Patient", Detail = "PatientId is required.", Status = 400 });
        }

        Guid performerId = _currentUserService.UserId ?? Guid.Empty;
        if (performerId == Guid.Empty)
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(claim, out var parsedClaim))
            {
                performerId = parsedClaim;
            }
            else
            {
                var fallback = await _context.Users.FirstOrDefaultAsync();
                if (fallback != null) performerId = fallback.Id;
            }
        }

        var procedure = new NursingProcedure
        {
            Id = Guid.NewGuid(),
            PatientId = dto.PatientId,
            AppointmentId = dto.AppointmentId,
            PerformedByUserId = performerId,
            ProcedureType = dto.ProcedureType,
            BodySite = dto.BodySite ?? string.Empty,
            Notes = dto.Notes ?? string.Empty,
            PatientTolerance = dto.PatientTolerance ?? "Well Tolerated",
            SuppliesUsed = dto.SuppliesUsed ?? string.Empty,
            Status = "Completed",
            Priority = "Routine",
            OrderedAt = DateTime.UtcNow,
            PerformedAt = dto.PerformedAt ?? DateTime.UtcNow,
            Price = 30.00m
        };

        _context.NursingProcedures.Add(procedure);

        if (dto.AppointmentId.HasValue && dto.AppointmentId.Value != Guid.Empty)
        {
            var appointment = await _context.Appointments.FindAsync(dto.AppointmentId.Value);
            if (appointment != null && (appointment.Status == Domain.Enums.AppointmentStatus.CheckedIn || appointment.Status == Domain.Enums.AppointmentStatus.TriageComplete))
            {
                appointment.Status = Domain.Enums.AppointmentStatus.InProcedure;
            }
        }

        await _context.SaveChangesAsync();

        var created = await _context.NursingProcedures
            .Include(p => p.Patient)
            .Include(p => p.PerformedByUser)
            .Include(p => p.OrderedByDoctor)
            .AsNoTracking()
            .FirstAsync(p => p.Id == procedure.Id);

        var resultDto = _mapper.Map<NursingProcedureResponseDto>(created);
        return CreatedAtAction(nameof(GetById), new { id = procedure.Id }, resultDto);
    }

    [HttpPost("orders")]
    [Authorize(Roles = "Doctor,Admin")]
    [EndpointSummary("Place an in-clinic nursing procedure order (by Doctor or Clinician)")]
    [ProducesResponseType(typeof(NursingProcedureResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> OrderProcedure([FromBody] OrderNursingProcedureRequestDto dto)
    {
        if (dto.PatientId == Guid.Empty)
        {
            return BadRequest(new ProblemDetails { Title = "Invalid Patient", Detail = "PatientId is required.", Status = 400 });
        }

        Guid doctorId = _currentUserService.UserId ?? Guid.Empty;
        if (doctorId == Guid.Empty)
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(claim, out var parsedClaim))
            {
                doctorId = parsedClaim;
            }
        }

        var procedure = new NursingProcedure
        {
            Id = Guid.NewGuid(),
            PatientId = dto.PatientId,
            AppointmentId = dto.AppointmentId,
            OrderedByDoctorId = doctorId != Guid.Empty ? doctorId : null,
            ProcedureType = dto.ProcedureType,
            BodySite = dto.BodySite ?? string.Empty,
            Notes = dto.Notes ?? string.Empty,
            PatientTolerance = "Pending Procedure",
            SuppliesUsed = string.Empty,
            Status = "Ordered",
            Priority = string.IsNullOrWhiteSpace(dto.Priority) ? "Routine" : dto.Priority,
            Price = dto.Price > 0 ? dto.Price : 30.00m,
            OrderedAt = DateTime.UtcNow
        };

        _context.NursingProcedures.Add(procedure);
        await _context.SaveChangesAsync();

        var created = await _context.NursingProcedures
            .Include(p => p.Patient)
            .Include(p => p.PerformedByUser)
            .Include(p => p.OrderedByDoctor)
            .AsNoTracking()
            .FirstAsync(p => p.Id == procedure.Id);

        var resultDto = _mapper.Map<NursingProcedureResponseDto>(created);
        return CreatedAtAction(nameof(GetById), new { id = procedure.Id }, resultDto);
    }

    [HttpPost("{id:guid}/complete")]
    [Authorize(Roles = "Nurse,Admin")]
    [EndpointSummary("Fulfill and complete an ordered nursing procedure (by Nurse)")]
    [ProducesResponseType(typeof(NursingProcedureResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CompleteProcedure(Guid id, [FromBody] CompleteNursingProcedureRequestDto dto)
    {
        var procedure = await _context.NursingProcedures
            .Include(p => p.Patient)
            .Include(p => p.OrderedByDoctor)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (procedure == null)
        {
            return NotFound(new ProblemDetails { Title = "Not Found", Detail = $"Nursing procedure {id} not found.", Status = 404 });
        }

        Guid nurseId = _currentUserService.UserId ?? Guid.Empty;
        if (nurseId == Guid.Empty)
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(claim, out var parsedClaim))
            {
                nurseId = parsedClaim;
            }
            else
            {
                var fallback = await _context.Users.FirstOrDefaultAsync();
                if (fallback != null) nurseId = fallback.Id;
            }
        }

        procedure.PerformedByUserId = nurseId;
        procedure.PerformedAt = DateTime.UtcNow;
        procedure.Status = "Completed";
        if (!string.IsNullOrWhiteSpace(dto.PatientTolerance))
        {
            procedure.PatientTolerance = dto.PatientTolerance;
        }
        if (!string.IsNullOrWhiteSpace(dto.SuppliesUsed))
        {
            procedure.SuppliesUsed = dto.SuppliesUsed;
        }
        if (!string.IsNullOrWhiteSpace(dto.Notes))
        {
            procedure.Notes = string.IsNullOrWhiteSpace(procedure.Notes)
                ? dto.Notes
                : $"{procedure.Notes} | Nurse completion note: {dto.Notes}";
        }

        await _context.SaveChangesAsync();

        var completed = await _context.NursingProcedures
            .Include(p => p.Patient)
            .Include(p => p.PerformedByUser)
            .Include(p => p.OrderedByDoctor)
            .AsNoTracking()
            .FirstAsync(p => p.Id == procedure.Id);

        var resultDto = _mapper.Map<NursingProcedureResponseDto>(completed);
        return Ok(resultDto);
    }
}
