namespace ClinicManagementSystem.Application.Interfaces;

public interface IAuditService
{
    Task LogAsync(string entityName, string entityId, string action, string? changes, string userId, string userName);
}
