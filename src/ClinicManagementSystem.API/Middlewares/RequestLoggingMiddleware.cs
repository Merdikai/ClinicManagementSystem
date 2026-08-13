using System.Diagnostics;
using Microsoft.Extensions.Logging;

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
        // Generate unique correlation ID for this request
        var correlationId = context.Request.Headers["X-Correlation-Id"].FirstOrDefault()
            ?? Guid.NewGuid().ToString();

        // Store in context so other middleware/controllers can access it
        context.Items["CorrelationId"] = correlationId;

        // Add to response headers so client can trace the request
        context.Response.Headers["X-Correlation-Id"] = correlationId;

        // Create scoped logging — every log inside this scope gets CorrelationId automatically
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId
        }))
        {
            var stopwatch = Stopwatch.StartNew();

            // Log request
            _logger.LogInformation(
                "[REQUEST] {Method} {Path}{QueryString} | User: {User}",
                context.Request.Method,
                context.Request.Path,
                context.Request.QueryString,
                context.User?.Identity?.Name ?? "anonymous"
            );

            // Capture original response body stream
            var originalBodyStream = context.Response.Body;
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            try
            {
                await _next(context);
            }
            finally
            {
                stopwatch.Stop();

                // Read response body for logging
                responseBody.Seek(0, SeekOrigin.Begin);
                var responseText = await new StreamReader(responseBody).ReadToEndAsync();
                responseBody.Seek(0, SeekOrigin.Begin);

                // Copy back to original stream
                await responseBody.CopyToAsync(originalBodyStream);
                context.Response.Body = originalBodyStream;

                // Truncate long responses for logging
                if (responseText.Length > 500)
                    responseText = responseText[..500] + "...";

                // Log response
                _logger.LogInformation(
                    "[RESPONSE] {StatusCode} | {Elapsed}ms | Body: {Body}",
                    context.Response.StatusCode,
                    stopwatch.ElapsedMilliseconds,
                    responseText
                );
            }
        }
    }
}