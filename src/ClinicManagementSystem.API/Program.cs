using Asp.Versioning;
using ClinicManagementSystem.API.Authorization;
using ClinicManagementSystem.API.Extensions;
using ClinicManagementSystem.API.Filters;
using ClinicManagementSystem.API.Hubs;
using ClinicManagementSystem.API.Middlewares;
using ClinicManagementSystem.API.Options;
using ClinicManagementSystem.API.Services;
using ClinicManagementSystem.Application;
using ClinicManagementSystem.Application.Interfaces;
using ClinicManagementSystem.Application.Options;
using ClinicManagementSystem.Application.Reports.Queries;
using ClinicManagementSystem.Application.Services;
using ClinicManagementSystem.Infrastructure;
using ClinicManagementSystem.Infrastructure.Identity;
using ClinicManagementSystem.Infrastructure.Persistence.Context;
using ClinicManagementSystem.Infrastructure.SeedData;
using ClinicManagementSystem.Infrastructure.Services;
using Hangfire;
using Hangfire.PostgreSql;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseDefaultServiceProvider((context, options) =>
{
    options.ValidateScopes = context.HostingEnvironment.IsDevelopment();
    options.ValidateOnBuild = context.HostingEnvironment.IsDevelopment();
});

// ─── Typed Validated Options (M4) ───
builder.Services.AddOptions<JwtOptions>()
    .BindConfiguration(JwtOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<EmailOptions>()
    .BindConfiguration(EmailOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<CorsOptions>()
    .BindConfiguration(CorsOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

// ─── Database ───
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection must be configured via User Secrets or environment variables.");

builder.Services.AddDbContext<ClinicDbContext>(options =>
    options.UseNpgsql(
        connectionString,
        npgsqlOptions => npgsqlOptions
            .EnableRetryOnFailure(3)
            .UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)
    ));

// ─── Application & Infrastructure Services ───
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// ─── CORS ───
var corsOptions = builder.Configuration.GetSection(CorsOptions.SectionName).Get<CorsOptions>();
var defaultOrigins = new[] { "http://localhost:4200", "http://localhost:5041", "http://localhost:8080" };
var allowedOrigins = (corsOptions?.AllowedOrigins is { Length: > 0 })
    ? corsOptions.AllowedOrigins
    : defaultOrigins;

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularDev", policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
            .SetPreflightMaxAge(TimeSpan.FromHours(1));
    });
});

// ─── Antiforgery (XSRF protection for HttpOnly refresh cookie) ───
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-XSRF-TOKEN";
    options.Cookie.Name = ".AspNetCore.Antiforgery.Clinic";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

// ─── Health Checks ───
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ClinicDbContext>();

// ─── API Versioning ───
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = ApiVersionReader.Combine(
        new UrlSegmentApiVersionReader(),
        new HeaderApiVersionReader("X-Api-Version")
    );
})
.AddMvc()
.AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'V";
    options.SubstituteApiVersionInUrl = true;
});

// ─── JWT Authentication ───
var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("Jwt configuration section is missing.");
if (string.IsNullOrWhiteSpace(jwtOptions.Key) || jwtOptions.Key.Length < 32)
    throw new InvalidOperationException("Jwt:Key must be configured with at least 32 characters via User Secrets or environment variables.");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ClockSkew = TimeSpan.FromSeconds(30),
        ValidIssuer = jwtOptions.Issuer,
        ValidAudience = jwtOptions.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key))
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

// ─── Register JWT helpers ───
builder.Services.AddScoped<JwtTokenGenerator>();
builder.Services.AddScoped<PasswordHasher>();

// ─── Rate Limiting ───
builder.Services.AddCustomRateLimiting();

// ─── Hangfire ───
builder.Services.AddHangfire(config =>
    config.UsePostgreSqlStorage(options =>
        options.UseNpgsqlConnection(connectionString)));
builder.Services.AddHangfireServer();

// ─── SignalR & Notifications ───
builder.Services.AddSignalR();
builder.Services.AddScoped<INotificationService, NotificationService>();

// ─── HybridCache (L1 in-memory + optional Redis L2 from M7) ───
var redisConnection = builder.Configuration["Redis:ConnectionString"];
if (!string.IsNullOrWhiteSpace(redisConnection))
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnection;
        options.InstanceName = "ClinicMgmt:";
    });
}

#pragma warning disable EXTEXP0018
builder.Services.AddHybridCache(options =>
{
    options.DefaultEntryOptions = new Microsoft.Extensions.Caching.Hybrid.HybridCacheEntryOptions
    {
        Expiration = TimeSpan.FromMinutes(10),
        LocalCacheExpiration = TimeSpan.FromMinutes(2)
    };
});
#pragma warning restore EXTEXP0018

builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

// ─── Centralized Exception Handling & RFC 9457 / 7807 ───
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// ─── Asynchronous Report Queue & Background Worker ───
builder.Services.AddSingleton<IAsyncReportQueue, AsyncReportQueue>();
builder.Services.AddHostedService<AsyncReportBackgroundWorker>();

// ─── Resource-Based Authorization (M12) ───
builder.Services.AddScoped<IAuthorizationHandler, AppointmentResourceHandler>();
builder.Services.AddScoped<IAuthorizationHandler, AppointmentDtoResourceHandler>();
builder.Services.AddScoped<IAuthorizationHandler, PatientResourceHandler>();
builder.Services.AddScoped<IAuthorizationHandler, InvoiceResourceHandler>();
builder.Services.AddScoped<IAuthorizationHandler, PrescriptionResourceHandler>();

// ─── Controllers & OpenAPI/Swagger ───
builder.Services.AddControllers(options =>
{
    options.Filters.Add<ValidationFilter>();
}).AddJsonOptions(options =>
{
    options.JsonSerializerOptions.DictionaryKeyPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
});

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(ms => ms.Value?.Errors.Count > 0)
            .ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray()
            );

        var problemDetails = new ProblemDetails
        {
            Title = "Validation Failed",
            Detail = "One or more validation errors occurred.",
            Status = StatusCodes.Status400BadRequest,
            Instance = context.HttpContext.Request.Path
        };
        problemDetails.Extensions.Add("errors", errors);

        return new BadRequestObjectResult(problemDetails)
        {
            ContentTypes = { "application/problem+json" }
        };
    };
});

builder.Services.AddEndpointsApiExplorer();
static void ConfigureOpenApiDocument(OpenApiOptions options, string versionLabel)
{
    options.AddDocumentTransformer((document, context, ct) =>
    {
        document.Info.Title = $"Clinic Management System API {versionLabel}";
        document.Info.Version = versionLabel;
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Enter JWT Bearer token."
        };
        document.Security ??= [];
        document.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = []
        });
        return Task.CompletedTask;
    });
}

builder.Services.AddOpenApi("v1", options => ConfigureOpenApiDocument(options, "v1"));
builder.Services.AddOpenApi("v2", options => ConfigureOpenApiDocument(options, "v2"));

var app = builder.Build();

// ─── Apply Pending Migrations / Seed Database ───
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ClinicDbContext>();
    await context.Database.MigrateAsync();
    await DbInitializer.SeedAsync(context);
}

// ─── Correlation ID before exception handling so errors always carry trace correlation ───
app.Use(async (context, next) =>
{
    var correlationId = context.Request.Headers["X-Correlation-Id"].FirstOrDefault() ?? Guid.NewGuid().ToString();
    context.Items["CorrelationId"] = correlationId;
    context.Response.Headers["X-Correlation-Id"] = correlationId;
    await next();
});

// ─── Middleware Pipeline ───
app.UseExceptionHandler();
app.UseStatusCodePages();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

// ─── Hardened Security Headers (M12) ───
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.Append("Permissions-Policy", "camera=(), microphone=(), geolocation=()");
    context.Response.Headers.Append("X-Permitted-Cross-Domain-Policies", "none");

    var isDocsRoute = context.Request.Path.StartsWithSegments("/openapi") ||
                      context.Request.Path.StartsWithSegments("/scalar");
    if (!isDocsRoute)
    {
        context.Response.Headers.Append(
            "Content-Security-Policy",
            "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data: https:; font-src 'self' data:; object-src 'none'; frame-ancestors 'none'; base-uri 'self';");
    }

    await next();
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("Clinic Management System API")
               .AddDocument("v1", "Clinic API v1", "/openapi/v1.json")
               .AddDocument("v2", "Clinic API v2", "/openapi/v2.json");
    });
}

app.UseResponseCompression();
app.UseStaticFiles();
app.UseDeprecationHeaders();
app.UseHttpsRedirection();
app.UseCors("AllowAngularDev");
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseLoginRateLimitPartitioning();
app.UseRateLimiter();
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = [new HangfireAdminAuthorizationFilter()]
});

app.MapHealthChecks("/health").DisableRateLimiting();
app.MapGet("/", () => Results.Ok("Clinic Management API is running"));
app.MapControllers();
app.MapHub<ClinicHub>("/hubs/clinic");

using (var scope = app.Services.CreateScope())
{
    var sender = scope.ServiceProvider.GetRequiredService<ISender>();
    RecurringJob.AddOrUpdate(
        "daily-revenue-report",
        () => sender.Send(new GetDailyRevenueQuery(DateTime.UtcNow), CancellationToken.None),
        Cron.Daily
    );
}

app.Run();

public partial class Program { }
