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
[Route("api/v{version:apiVersion}/shift-handovers")]
[Tags("Shift Handovers (SBAR)")]
[Authorize]
public class ShiftHandoversController : ControllerBase
{
    private readonly IClinicDbContext _context;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUserService;

    public ShiftHandoversController(
        IClinicDbContext context,
        IMapper mapper,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _mapper = mapper;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    [EndpointSummary("Get list of SBAR shift handovers")]
    [ProducesResponseType(typeof(IEnumerable<ShiftHandoverResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] DateTime? date = null,
        [FromQuery] string? shiftType = null)
    {
        var query = _context.ShiftHandovers
            .Include(s => s.NurseUser)
            .AsNoTracking()
            .AsQueryable();

        if (date.HasValue)
        {
            var start = date.Value.Date;
            var end = start.AddDays(1);
            query = query.Where(s => s.HandoverTimestamp >= start && s.HandoverTimestamp < end);
        }

        if (!string.IsNullOrWhiteSpace(shiftType))
        {
            query = query.Where(s => s.ShiftType == shiftType);
        }

        var items = await query
            .OrderByDescending(s => s.HandoverTimestamp)
            .ToListAsync();

        var dtos = _mapper.Map<IEnumerable<ShiftHandoverResponseDto>>(items);
        return Ok(dtos);
    }

    [HttpGet("latest")]
    [EndpointSummary("Get most recent shift handover")]
    [ProducesResponseType(typeof(ShiftHandoverResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> GetLatest()
    {
        var item = await _context.ShiftHandovers
            .Include(s => s.NurseUser)
            .AsNoTracking()
            .OrderByDescending(s => s.HandoverTimestamp)
            .FirstOrDefaultAsync();

        if (item == null)
        {
            return NoContent();
        }

        return Ok(_mapper.Map<ShiftHandoverResponseDto>(item));
    }

    [HttpPost]
    [Authorize(Roles = "Nurse,Admin")]
    [EndpointSummary("Create a new SBAR shift handover note")]
    [ProducesResponseType(typeof(ShiftHandoverResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateShiftHandoverDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.ShiftType) || string.IsNullOrWhiteSpace(dto.Situation))
        {
            return BadRequest(new ProblemDetails { Title = "Validation Error", Detail = "ShiftType and Situation are required.", Status = 400 });
        }

        Guid authorId = _currentUserService.UserId ?? Guid.Empty;
        if (authorId == Guid.Empty)
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(claim, out var parsedClaim))
            {
                authorId = parsedClaim;
            }
            else
            {
                var fallback = await _context.Users.FirstOrDefaultAsync();
                if (fallback != null) authorId = fallback.Id;
            }
        }

        var handover = new ShiftHandover
        {
            Id = Guid.NewGuid(),
            NurseUserId = authorId,
            ShiftType = dto.ShiftType,
            HandoverTimestamp = dto.HandoverTimestamp ?? DateTime.UtcNow,
            Situation = dto.Situation,
            Background = dto.Background ?? string.Empty,
            Assessment = dto.Assessment ?? string.Empty,
            Recommendation = dto.Recommendation ?? string.Empty,
            ActivePatientCount = dto.ActivePatientCount,
            CriticalAlertsCount = dto.CriticalAlertsCount
        };

        _context.ShiftHandovers.Add(handover);
        await _context.SaveChangesAsync();

        var created = await _context.ShiftHandovers
            .Include(s => s.NurseUser)
            .AsNoTracking()
            .FirstAsync(s => s.Id == handover.Id);

        var resultDto = _mapper.Map<ShiftHandoverResponseDto>(created);
        return CreatedAtAction(nameof(GetLatest), new { }, resultDto);
    }
}
