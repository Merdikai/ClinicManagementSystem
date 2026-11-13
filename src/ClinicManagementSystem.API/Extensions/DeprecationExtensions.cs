namespace ClinicManagementSystem.API.Extensions;

public static class DeprecationExtensions
{
    public static IApplicationBuilder UseDeprecationHeaders(this IApplicationBuilder app)
    {
        app.Use(async (context, next) =>
        {
            var path = context.Request.Path;

            // Only flag v1 endpoints that have explicit v2 successor implementations
            string? successorPath = path switch
            {
                _ when path.StartsWithSegments("/api/v1/patients", StringComparison.OrdinalIgnoreCase)
                    => path.Value!.Replace("/api/v1/patients", "/api/v2/patients", StringComparison.OrdinalIgnoreCase),
                _ when path.StartsWithSegments("/api/v1/appointments", StringComparison.OrdinalIgnoreCase)
                    => path.Value!.Replace("/api/v1/appointments", "/api/v2/appointments", StringComparison.OrdinalIgnoreCase),
                _ when path.StartsWithSegments("/api/v1/reports", StringComparison.OrdinalIgnoreCase)
                    => path.Value!.Replace("/api/v1/reports", "/api/v2/reports", StringComparison.OrdinalIgnoreCase),
                _ => null
            };

            if (!string.IsNullOrEmpty(successorPath))
            {
                context.Response.Headers["Deprecation"] = "true";
                context.Response.Headers["Sunset"] = "Fri, 31 Dec 2027 23:59:59 GMT";
                context.Response.Headers.Append("Link", $"<{successorPath}>; rel=\"successor-version\"");
            }

            await next();
        });

        return app;
    }
}
