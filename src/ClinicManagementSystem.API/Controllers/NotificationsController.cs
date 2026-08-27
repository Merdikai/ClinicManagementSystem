using Asp.Versioning;
using ClinicManagementSystem.Application.Interfaces;
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
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [AllowAnonymous]
    [HttpPost("test")]
    public async Task<IActionResult> TestNotification([FromBody] BroadcastAlertRequest? request = null)
    {
        var userClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var userId = Guid.TryParse(userClaim, out var parsed) ? parsed : Guid.NewGuid();
        var userName = User.Identity?.Name ?? "Admin";
        var messageText = !string.IsNullOrWhiteSpace(request?.Message) 
            ? request.Message 
            : $"Broadcast alert issued by {userName}";

        await _notificationService.NotifyAppointmentBookedAsync(
            userId,
            Guid.NewGuid(),
            messageText
        );

        return Ok(new { message = "Broadcast notification sent successfully", text = messageText });
    }
}
