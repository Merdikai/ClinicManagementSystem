using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Asp.Versioning;
using ClinicManagementSystem.Application.Common;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.Inquiries.Commands;
using ClinicManagementSystem.Application.Inquiries.Queries;
using ClinicManagementSystem.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/visitor-inquiries")]
[Tags("Visitor Inquiries")]
[Authorize(Roles = "Admin,Receptionist,Nurse,Doctor")]
public class VisitorInquiriesController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IClinicDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public VisitorInquiriesController(
        ISender sender,
        IClinicDbContext context,
        ICurrentUserService currentUserService)
    {
        _sender = sender;
        _context = context;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    [EndpointSummary("Get all visitor inquiries and front desk tracker tickets")]
    [ProducesResponseType(typeof(List<VisitorInquiryResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInquiries([FromQuery] string? status = null)
    {
        var list = await _sender.Send(new GetVisitorInquiriesQuery(status));
        return Ok(list);
    }

    [HttpPost]
    [EndpointSummary("Record a new visitor or telephone inquiry")]
    [ProducesResponseType(typeof(VisitorInquiryResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateInquiry([FromBody] CreateVisitorInquiryDto dto)
    {
        var userId = await ResolveUserIdAsync();

        var command = new CreateVisitorInquiryCommand(
            dto.VisitorName,
            dto.PhoneNumber,
            dto.InquiryType,
            dto.DepartmentTarget,
            dto.Notes,
            userId
        );

        var result = await _sender.Send(command);

        return result.Match<IActionResult>(
            onSuccess: item => Created(string.Empty, item),
            onFailure: (err, _) => BadRequest(new ProblemDetails { Title = "Inquiry Recording Failed", Detail = err, Status = 400 })
        );
    }

    [HttpPatch("{id:guid}/resolve")]
    [EndpointSummary("Mark a visitor inquiry as resolved or answered")]
    [ProducesResponseType(typeof(VisitorInquiryResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResolveInquiry(Guid id, [FromBody] ResolveInquiryRequestDto? dto = null)
    {
        var result = await _sender.Send(new ResolveVisitorInquiryCommand(id, dto?.ResolutionNotes));

        return result.Match<IActionResult>(
            onSuccess: item => Ok(item),
            onFailure: (err, _) => BadRequest(new ProblemDetails { Title = "Resolve Failed", Detail = err, Status = 400 })
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
            var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == emailClaim);
            if (user != null) return user.Id;
        }

        throw new UnauthorizedAccessException("Authenticated user identity could not be resolved.");
    }
}

public class ResolveInquiryRequestDto
{
    public string? ResolutionNotes { get; set; }
}
