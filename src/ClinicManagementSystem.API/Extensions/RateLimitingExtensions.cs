using ClinicManagementSystem.API.Constants;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.RateLimiting;

namespace ClinicManagementSystem.API.Extensions;

public static class RateLimitingExtensions
{
    public const string LoginPartitionItemKey = "LoginPartitionKey";

    public static IApplicationBuilder UseLoginRateLimitPartitioning(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            if (HttpMethods.IsPost(context.Request.Method) &&
                context.Request.Path.Value?.EndsWith("/auth/login", StringComparison.OrdinalIgnoreCase) == true &&
                context.Request.ContentType?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true)
            {
                context.Request.EnableBuffering();
                try
                {
                    using var doc = await JsonDocument.ParseAsync(context.Request.Body, cancellationToken: context.RequestAborted);
                    if (doc.RootElement.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var prop in doc.RootElement.EnumerateObject())
                        {
                            if (string.Equals(prop.Name, "username", StringComparison.OrdinalIgnoreCase) &&
                                prop.Value.ValueKind == JsonValueKind.String)
                            {
                                var username = prop.Value.GetString()?.Trim().ToLowerInvariant();
                                if (!string.IsNullOrEmpty(username))
                                {
                                    var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                                    context.Items[LoginPartitionItemKey] = $"login:{ip}:{username}";
                                }
                                break;
                            }
                        }
                    }
                }
                catch
                {
                    // Ignore malformed JSON; model binding/FluentValidation will return 400.
                }
                finally
                {
                    context.Request.Body.Position = 0;
                }
            }

            await next();
        });
    }

    public static IServiceCollection AddCustomRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // ─── Partitioned by (IP, Username) so sequential multi-role logins work while blocking brute-force ───
            options.AddPolicy("LoginPolicy", httpContext =>
            {
                var partitionKey = httpContext.Items[LoginPartitionItemKey] as string
                    ?? $"login:{httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: partitionKey,
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 8,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                        AutoReplenishment = true
                    });
            });

            options.AddPolicy("RefreshPolicy", httpContext =>
            {
                var partitionKey = $"refresh:{httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";
                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: partitionKey,
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 20,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                        AutoReplenishment = true
                    });
            });

            // ─── Anonymous Policy (No Authentication) ───
            options.AddPolicy(RateLimitingConstants.AnonymousPolicy, httpContext =>
                RateLimitPartition.GetTokenBucketLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                    factory: _ => new TokenBucketRateLimiterOptions
                    {
                        TokenLimit = RateLimitingConstants.AnonymousLimit,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0,
                        ReplenishmentPeriod = TimeSpan.FromSeconds(RateLimitingConstants.AnonymousWindowSeconds),
                        TokensPerPeriod = RateLimitingConstants.AnonymousLimit,
                        AutoReplenishment = true
                    }));

            // ─── Patient Policy (Authenticated Patients) ───
            options.AddPolicy(RateLimitingConstants.PatientPolicy, httpContext =>
                RateLimitPartition.GetTokenBucketLimiter(
                    partitionKey: httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "patient",
                    factory: _ => new TokenBucketRateLimiterOptions
                    {
                        TokenLimit = RateLimitingConstants.PatientLimit,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0,
                        ReplenishmentPeriod = TimeSpan.FromSeconds(RateLimitingConstants.PatientWindowSeconds),
                        TokensPerPeriod = RateLimitingConstants.PatientLimit,
                        AutoReplenishment = true
                    }));

            // ─── Staff Policy (Doctors, Nurses, Receptionists, Pharmacists) ───
            options.AddPolicy(RateLimitingConstants.StaffPolicy, httpContext =>
                RateLimitPartition.GetTokenBucketLimiter(
                    partitionKey: httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "staff",
                    factory: _ => new TokenBucketRateLimiterOptions
                    {
                        TokenLimit = RateLimitingConstants.StaffLimit,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0,
                        ReplenishmentPeriod = TimeSpan.FromSeconds(RateLimitingConstants.StaffWindowSeconds),
                        TokensPerPeriod = RateLimitingConstants.StaffLimit,
                        AutoReplenishment = true
                    }));

            // ─── Admin Policy ───
            options.AddPolicy(RateLimitingConstants.AdminPolicy, httpContext =>
                RateLimitPartition.GetTokenBucketLimiter(
                    partitionKey: httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "admin",
                    factory: _ => new TokenBucketRateLimiterOptions
                    {
                        TokenLimit = RateLimitingConstants.AdminLimit,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0,
                        ReplenishmentPeriod = TimeSpan.FromSeconds(RateLimitingConstants.AdminWindowSeconds),
                        TokensPerPeriod = RateLimitingConstants.AdminLimit,
                        AutoReplenishment = true
                    }));

            // ─── Report Concurrency Policy ───
            options.AddPolicy("report_concurrency", httpContext =>
                RateLimitPartition.GetConcurrencyLimiter(
                    partitionKey: httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous",
                    factory: _ => new ConcurrencyLimiterOptions
                    {
                        PermitLimit = 2,
                        QueueLimit = 5,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                    }));

            // ─── Global Fallback ───
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
                httpContext =>
                {
                    var path = httpContext.Request.Path;
                    // Auth endpoints enforce dedicated partitioned LoginPolicy/RefreshPolicy; docs & health stay unrestricted
                    if (path.StartsWithSegments("/health") ||
                        path.StartsWithSegments("/openapi") ||
                        path.StartsWithSegments("/scalar") ||
                        path.Value?.EndsWith("/auth/login", StringComparison.OrdinalIgnoreCase) == true ||
                        path.Value?.EndsWith("/auth/refresh", StringComparison.OrdinalIgnoreCase) == true ||
                        path.Value?.EndsWith("/auth/register", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        return RateLimitPartition.GetNoLimiter("bypass-global");
                    }

                    var role = httpContext.User.FindFirst(ClaimTypes.Role)?.Value
                        ?? httpContext.User.FindFirst("role")?.Value
                        ?? httpContext.User.FindFirst("http://schemas.microsoft.com/ws/2008/06/identity/claims/role")?.Value
                        ?? "anonymous";
                    var partitionKey = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous";

                    return RateLimitPartition.GetTokenBucketLimiter(partitionKey, _ =>
                    {
                        var (tokenLimit, windowSeconds) = role.ToLower() switch
                        {
                            "admin" => (RateLimitingConstants.AdminLimit, RateLimitingConstants.AdminWindowSeconds),
                            "doctor" or "nurse" or "receptionist" or "pharmacist" or "labtechnician" or "lab-technician" or "radiologist" or "accountant"
                                => (RateLimitingConstants.StaffLimit, RateLimitingConstants.StaffWindowSeconds),
                            "patient" => (RateLimitingConstants.PatientLimit, RateLimitingConstants.PatientWindowSeconds),
                            _ => (RateLimitingConstants.AnonymousLimit, RateLimitingConstants.AnonymousWindowSeconds)
                        };

                        return new TokenBucketRateLimiterOptions
                        {
                            TokenLimit = tokenLimit,
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 0,
                            ReplenishmentPeriod = TimeSpan.FromSeconds(windowSeconds),
                            TokensPerPeriod = tokenLimit,
                            AutoReplenishment = true
                        };
                    });
                });

            // ─── Return Dynamic Retry-After Header and ProblemDetails ───
            options.OnRejected = async (context, cancellationToken) =>
            {
                var retryAfter = "10";
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var ts))
                {
                    retryAfter = Math.Max(1, (int)ts.TotalSeconds).ToString();
                }

                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.HttpContext.Response.Headers.RetryAfter = retryAfter;
                context.HttpContext.Response.ContentType = "application/problem+json";

                var problem = new ProblemDetails
                {
                    Title = "Rate limit exceeded",
                    Detail = $"Too many requests. Retry after {retryAfter} seconds.",
                    Status = StatusCodes.Status429TooManyRequests,
                    Type = "https://tools.ietf.org/html/rfc6585#section-4",
                    Instance = context.HttpContext.Request.Path
                };

                await context.HttpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
            };
        });

        return services;
    }
}
