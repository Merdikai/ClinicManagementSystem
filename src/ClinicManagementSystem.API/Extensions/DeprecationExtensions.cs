namespace ClinicManagementSystem.API.Extensions;

public static class DeprecationExtensions
{
    public static IApplicationBuilder UseDeprecationHeaders(this IApplicationBuilder app)
    {
        app.Use(async (context, next) =>
        {
            // Check if path contains "v1" and we want to deprecate it
            if (context.Request.Path.StartsWithSegments("/api/v1"))
            {
                context.Response.Headers["Deprecation"] = "false";
                context.Response.Headers["Sunset"] = "2027-12-31";
            }

            await next();
        });

        return app;
    }
}
