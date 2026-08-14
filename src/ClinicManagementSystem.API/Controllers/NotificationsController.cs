using Asp.Versioning;
using ClinicManagementSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagementSystem.API.Controllers;

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

    [HttpPost("test")]
    public async Task<IActionResult> TestNotification()
    {
        var userId = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
        var userName = User.Identity?.Name ?? "Unknown";

        await _notificationService.NotifyAppointmentBookedAsync(
            userId,
            Guid.NewGuid(),
            $"Test notification for {userName}"
        );

        return Ok(new { message = "Test notification sent" });
    }
}
