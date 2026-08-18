using ClinicManagementSystem.Application.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace ClinicManagementSystem.API.Hubs;

public class ClinicHub : Hub<IClinicHubClient>
{
    public override async Task OnConnectedAsync()
    {
        var role = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        if (role is not null)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, role);
        }

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

    public async Task SubscribeToPatient(Guid patientId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"patient-{patientId}");
    }

    public async Task UnsubscribeFromPatient(Guid patientId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"patient-{patientId}");
    }
}
