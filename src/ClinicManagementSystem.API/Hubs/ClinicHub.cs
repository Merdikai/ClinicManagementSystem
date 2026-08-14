using Microsoft.AspNetCore.SignalR;

namespace ClinicManagementSystem.API.Hubs;

public class ClinicHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        // Add user to their role group
        var role = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        if (role is not null)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, role);
        }

        // Add user to their personal group (for targeted notifications)
        var userId = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userId is not null)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var role = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        if (role is not null)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, role);
        }

        var userId = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userId is not null)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user-{userId}");
        }

        await base.OnDisconnectedAsync(exception);
    }

    // Client can call this to subscribe to specific patient updates
    public async Task SubscribeToPatient(Guid patientId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"patient-{patientId}");
    }

    public async Task UnsubscribeFromPatient(Guid patientId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"patient-{patientId}");
    }
}
