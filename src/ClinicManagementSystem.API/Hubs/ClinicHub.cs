using System.Security.Claims;
using ClinicManagementSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.API.Hubs;

[Authorize]
public class ClinicHub : Hub<IClinicHubClient>
{
    private readonly IClinicDbContext _context;

    public ClinicHub(IClinicDbContext context)
    {
        _context = context;
    }

    public override async Task OnConnectedAsync()
    {
        if (Context.User is not null)
        {
            var roles = Context.User.FindAll(ClaimTypes.Role)
                .Select(c => c.Value)
                .Concat(Context.User.FindAll("role").Select(c => c.Value))
                .Where(r => !string.IsNullOrWhiteSpace(r))
                .Distinct(StringComparer.OrdinalIgnoreCase);

            foreach (var role in roles)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, role);
            }

            var userId = Context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrWhiteSpace(userId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");
            }
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (Context.User is not null)
        {
            var roles = Context.User.FindAll(ClaimTypes.Role)
                .Select(c => c.Value)
                .Concat(Context.User.FindAll("role").Select(c => c.Value))
                .Where(r => !string.IsNullOrWhiteSpace(r))
                .Distinct(StringComparer.OrdinalIgnoreCase);

            foreach (var role in roles)
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, role);
            }

            var userId = Context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrWhiteSpace(userId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user-{userId}");
            }
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task SubscribeToPatient(Guid patientId)
    {
        var user = Context.User ?? throw new HubException("Unauthorized");

        var isClinicalStaff = user.IsInRole("Admin") || user.IsInRole("Doctor") ||
                              user.IsInRole("Nurse") || user.IsInRole("Receptionist") ||
                              user.IsInRole("Pharmacist") || user.IsInRole("LabTechnician") ||
                              user.IsInRole("Radiologist");

        if (!isClinicalStaff)
        {
            if (!user.IsInRole("Patient"))
            {
                throw new HubException("Forbidden: insufficient role permissions to subscribe to patient updates.");
            }

            var email = user.FindFirst(ClaimTypes.Email)?.Value ?? user.FindFirst("email")?.Value;
            if (string.IsNullOrWhiteSpace(email))
            {
                throw new HubException("Forbidden: missing email claim for patient verification.");
            }

            var ownsPatient = await _context.Patients
                .AsNoTracking()
                .AnyAsync(p => p.Id == patientId && p.Email == email);

            if (!ownsPatient)
            {
                throw new HubException("Forbidden: patients may only subscribe to their own record.");
            }
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, $"patient-{patientId}");
    }

    public async Task UnsubscribeFromPatient(Guid patientId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"patient-{patientId}");
    }
}
