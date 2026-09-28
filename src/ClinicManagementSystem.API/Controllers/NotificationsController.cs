using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Asp.Versioning;
using AutoMapper;
using ClinicManagementSystem.Application.Notifications.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagementSystem.API.Controllers;

public record BroadcastAlertRequest(string? Message, string? Title);

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/notifications")]
[Tags("Notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IMapper _mapper;

    public NotificationsController(ISender sender, IMapper mapper)
    {
        _sender = sender;
        _mapper = mapper;
    }

    [AllowAnonymous]
    [HttpPost("test")]
    public async Task<IActionResult> TestNotification([FromBody] BroadcastAlertRequest? request = null)
    {
        var userClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userId = Guid.TryParse(userClaim, out var parsed) ? parsed : Guid.NewGuid();
        var userName = User.Identity?.Name ?? "Admin";
        var messageText = !string.IsNullOrWhiteSpace(request?.Message) 
            ? request.Message 
            : $"Broadcast alert issued by {userName}";

        var result = await _sender.Send(new SendTestNotificationCommand(userId, messageText));
        return Ok(new { message = "Broadcast notification sent successfully", text = result });
    }
}
