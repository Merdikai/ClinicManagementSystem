using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Asp.Versioning;
using ClinicManagementSystem.Application.Common;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.Interfaces;
using ClinicManagementSystem.Application.Reconciliation.Commands;
using ClinicManagementSystem.Application.Reconciliation.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/daily-reconciliations")]
[Tags("Daily Cash Reconciliation")]
[Authorize]
public class DailyReconciliationsController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IClinicDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public DailyReconciliationsController(
        ISender sender,
        IClinicDbContext context,
        ICurrentUserService currentUserService)
    {
        _sender = sender;
        _context = context;
        _currentUserService = currentUserService;
    }

    [HttpGet("today-summary")]
    [EndpointSummary("Get today's cash, card, and digital payment collections summary")]
    [ProducesResponseType(typeof(DailyCashSummaryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTodaySummary()
    {
        var userId = await ResolveUserIdAsync();
        var summary = await _sender.Send(new GetTodayReconciliationSummaryQuery(userId));
        return Ok(summary);
    }

    [HttpGet]
    [EndpointSummary("Get historical daily cash reconciliation drawer logs")]
    [ProducesResponseType(typeof(List<DailyCashReconciliationResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetReconciliations([FromQuery] int limit = 30)
    {
        var list = await _sender.Send(new GetDailyCashReconciliationsQuery(limit));
        return Ok(list);
    }

    [HttpPost]
    [EndpointSummary("Submit daily cash drawer closing and reconciliation report")]
    [ProducesResponseType(typeof(DailyCashReconciliationResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateReconciliation([FromBody] CreateDailyCashReconciliationDto dto)
    {
        var userId = await ResolveUserIdAsync();

        var command = new CreateDailyCashReconciliationCommand(
            userId,
            dto.Shift,
            dto.CashCollected,
            dto.CardCollected,
            dto.DigitalCollected,
            dto.TotalCollected,
            dto.ClosingCashCounted,
            dto.Notes
        );

        var result = await _sender.Send(command);

        return result.Match<IActionResult>(
            onSuccess: item => Created(string.Empty, item),
            onFailure: (err, _) => BadRequest(new ProblemDetails { Title = "Reconciliation Failed", Detail = err, Status = 400 })
        );
    }

    [HttpPatch("{id:guid}/audit")]
    [EndpointSummary("Audit and verify front-desk receptionist daily cash drawer closing")]
    [ProducesResponseType(typeof(DailyCashReconciliationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AuditReconciliation(
        [FromRoute] Guid id,
        [FromBody] ClinicManagementSystem.Application.Finance.DTOs.AuditDailyCashReconciliationDto dto)
    {
        var auditorId = await ResolveUserIdAsync();
        var auditor = await _context.Users.FindAsync(auditorId);
        var auditorName = auditor != null ? $"{auditor.FirstName} {auditor.LastName}".Trim() : "Accountant Dave";

        var command = new ClinicManagementSystem.Application.Finance.Commands.AuditDailyCashReconciliationCommand(id, dto, auditorId, auditorName);
        var result = await _sender.Send(command);

        return result.Match<IActionResult>(
            onSuccess: Ok,
            onFailure: (err, _) => BadRequest(new ProblemDetails { Title = "Audit Failed", Detail = err, Status = 400 })
        );
    }

    private async Task<Guid> ResolveUserIdAsync()
    {
        if (_currentUserService.UserId.HasValue && _currentUserService.UserId.Value != Guid.Empty)
        {
            return _currentUserService.UserId.Value;
        }

        var emailClaim = User.FindFirst(ClaimTypes.Email)?.Value 
            ?? User.FindFirst("email")?.Value 
            ?? User.FindFirst(ClaimTypes.Name)?.Value;

        if (!string.IsNullOrWhiteSpace(emailClaim))
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == emailClaim);
            if (user != null) return user.Id;
        }

        var receptionistUser = await _context.Users.FirstOrDefaultAsync(u => u.Email.Contains("receptionist"));
        if (receptionistUser != null) return receptionistUser.Id;

        var anyUser = await _context.Users.FirstOrDefaultAsync();
        return anyUser?.Id ?? Guid.NewGuid();
    }
}
