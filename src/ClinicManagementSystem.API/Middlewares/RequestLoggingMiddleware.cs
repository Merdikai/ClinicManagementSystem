using System.Diagnostics;
using System.Security.Claims;

namespace ClinicManagementSystem.API.Middlewares;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Items["CorrelationId"] as string
            ?? context.Request.Headers["X-Correlation-Id"].FirstOrDefault()
            ?? Guid.NewGuid().ToString();

        context.Items["CorrelationId"] = correlationId;
        context.Response.Headers["X-Correlation-Id"] = correlationId;

        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId
        }))
        {
            var stopwatch = Stopwatch.StartNew();
            var userLabel = context.User?.Identity?.Name
                ?? context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? "anonymous";

            _logger.LogInformation(
                "[REQUEST] {Method} {Path}{QueryString} | User: {User}",
                context.Request.Method,
                context.Request.Path,
                context.Request.QueryString,
                userLabel);

            try
            {
                await _next(context);
            }
            finally
            {
                stopwatch.Stop();
                _logger.LogInformation(
                    "[RESPONSE] {Method} {Path} -> {StatusCode} | {Elapsed}ms | User: {User} | ContentType: {ContentType}",
                    context.Request.Method,
                    context.Request.Path,
                    context.Response.StatusCode,
                    stopwatch.ElapsedMilliseconds,
                    userLabel,
                    context.Response.ContentType ?? "none");
            }
        }
    }
}
