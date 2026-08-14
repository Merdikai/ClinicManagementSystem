using ClinicManagementSystem.API.Constants;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;
using System.Threading.RateLimiting;

namespace ClinicManagementSystem.API.Extensions;

public static class RateLimitingExtensions
{
    public static IServiceCollection AddCustomRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
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

            // ─── Global Fallback ───
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
                httpContext =>
                {
                    var role = httpContext.User.FindFirst("http://schemas.microsoft.com/ws/2008/06/identity/claims/role")?.Value ?? "anonymous";
                    var partitionKey = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous";

                    return RateLimitPartition.GetTokenBucketLimiter(partitionKey, _ =>
                    {
                        // Determine limits based on role
                        var (tokenLimit, windowSeconds) = role.ToLower() switch
                        {
                            "admin" => (RateLimitingConstants.AdminLimit, RateLimitingConstants.AdminWindowSeconds),
                            "doctor" or "nurse" or "receptionist" or "pharmacist" 
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

            // ─── Return Retry-After Header ───
            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.HttpContext.Response.Headers.RetryAfter = 
                    context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter) 
                        ? retryAfter.TotalSeconds.ToString() 
                        : "10";

                await context.HttpContext.Response.WriteAsync(
                    "{\"title\":\"Too Many Requests\",\"status\":429,\"detail\":\"Rate limit exceeded. Please try again later.\"}",
                    cancellationToken);
            };
        });

        return services;
    }
}
