using System.Security.Claims;
using ClinicManagementSystem.Application.Interfaces;
using ClinicManagementSystem.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ClinicManagementSystem.API.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class AuditActionAttribute : TypeFilterAttribute
{
    public AuditActionAttribute(string entityName, string action)
        : base(typeof(AuditLogFilter))
    {
        Arguments = [entityName, action];
    }
}

public sealed class AuditLogFilter : IAsyncActionFilter
{
    private readonly IClinicDbContext _db;
    private readonly string _entityName;
    private readonly string _action;

    public AuditLogFilter(IClinicDbContext db, string entityName, string action)
    {
        _db = db;
        _entityName = entityName;
        _action = action;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var executed = await next();

        if (executed.Exception is not null && !executed.ExceptionHandled)
            return;

        if (executed.Result is ObjectResult objResult && objResult.StatusCode >= 400)
            return;
        if (executed.Result is StatusCodeResult statusResult && statusResult.StatusCode >= 400)
            return;

        var user = context.HttpContext.User;
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? user.FindFirst("sub")?.Value
                     ?? "system";
        var userName = user.Identity?.Name ?? "Anonymous";

        var entityId = context.RouteData.Values
            .Where(kv => kv.Key.Contains("id", StringComparison.OrdinalIgnoreCase))
            .Select(kv => kv.Value?.ToString())
            .FirstOrDefault(v => !string.IsNullOrWhiteSpace(v))
            ?? context.HttpContext.Request.Path.Value
            ?? "N/A";

        var details = $"HTTP {context.HttpContext.Request.Method} {context.HttpContext.Request.Path}{context.HttpContext.Request.QueryString}";

        _db.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            EntityName = _entityName,
            EntityId = entityId,
            Action = _action,
            Changes = details.Length > 500 ? details[..500] : details,
            UserId = userId,
            UserName = userName,
            Timestamp = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(context.HttpContext.RequestAborted);
    }
}

