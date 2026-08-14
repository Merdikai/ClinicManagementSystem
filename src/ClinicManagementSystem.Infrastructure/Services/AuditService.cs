using ClinicManagementSystem.Application.Interfaces;
using ClinicManagementSystem.Domain.Entities;
using ClinicManagementSystem.Infrastructure.Persistence.Context;

namespace ClinicManagementSystem.Infrastructure.Services;

public class AuditService : IAuditService
{
    private readonly ClinicDbContext _context;

    public AuditService(ClinicDbContext context)
    {
        _context = context;
    }

    public async Task LogAsync(string entityName, string entityId, string action, string? changes, string userId, string userName)
    {
        var auditLog = new AuditLog
        {
            EntityName = entityName,
            EntityId = entityId,
            Action = action,
            Changes = changes,
            UserId = userId,
            UserName = userName
        };

        _context.AuditLogs.Add(auditLog);
        await _context.SaveChangesAsync();
    }
}
