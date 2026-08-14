using ClinicManagementSystem.Application;
using ClinicManagementSystem.Infrastructure;
using ClinicManagementSystem.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using ClinicManagementSystem.API.Middlewares;
using ClinicManagementSystem.API.Filters;
using ClinicManagementSystem.Infrastructure.SeedData;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using ClinicManagementSystem.Infrastructure.Identity;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Mvc;
using ClinicManagementSystem.Application.Interfaces;
using ClinicManagementSystem.API.Extensions;
using Asp.Versioning;
using Hangfire;
using Hangfire.PostgreSql;
using MediatR;
using ClinicManagementSystem.Application.Reports.Queries;
using ClinicManagementSystem.API.Hubs;
using ClinicManagementSystem.API.Services;

var builder = WebApplication.CreateBuilder(args);


// ─── Database ───
builder.Services.AddDbContext<ClinicDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions => npgsqlOptions.EnableRetryOnFailure(3)
    ));

// ─── Application & Infrastructure Services ───
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices();

// ─── CORS ───
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularDev", policy =>
    {
        policy.WithOrigins(
                "http://localhost:4200",      // Angular dev server
                "http://localhost:5041"       // Swagger
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
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
var jwtKey = builder.Configuration["Jwt:Key"]!;
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
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtKey))
    };
});

// ─── Register JWT helpers ───
builder.Services.AddScoped<JwtTokenGenerator>();
builder.Services.AddScoped<PasswordHasher>();
//builder.Services.AddScoped<IAuthService, AuthService>();

// ─── Database ... (already exists)


// ─── Rate Limiting ───
builder.Services.AddCustomRateLimiting();

// ─── Hangfire ───
builder.Services.AddHangfire(config =>
    config.UsePostgreSqlStorage(options => 
        options.UseNpgsqlConnection(builder.Configuration.GetConnectionString("DefaultConnection"))));
builder.Services.AddHangfireServer();


// ─── SignalR & Notifications ───
builder.Services.AddSignalR();
builder.Services.AddScoped<INotificationService, NotificationService>();

// ─── HybridCache ───
#pragma warning disable EXTEXP0018
builder.Services.AddHybridCache(options =>
{
    options.DefaultEntryOptions = new Microsoft.Extensions.Caching.Hybrid.HybridCacheEntryOptions
    {
        Expiration = TimeSpan.FromMinutes(5),
        LocalCacheExpiration = TimeSpan.FromMinutes(5)
    };
});
#pragma warning restore EXTEXP0018

// ─── Controllers & Swagger ───
builder.Services.AddControllers(options =>
{
    options.Filters.Add<ValidationFilter>();
});



// ─── ProblemDetails for validation errors ───
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
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer' followed by a space and your JWT token."
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// ─── Seed Database ───
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ClinicDbContext>();
    await DbInitializer.SeedAsync(context);
}


// ─── Middleware Pipeline ───
app.UseMiddleware<RequestLoggingMiddleware>();   // ← First: log everything
app.UseMiddleware<ExceptionMiddleware>();        // ← Second: catch exceptions

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "ClinicManagementSystem.API v1");
        c.DocumentTitle = "Clinic Management System API";
        c.DisplayRequestDuration();
        c.EnableDeepLinking();
        c.ConfigObject = new Swashbuckle.AspNetCore.SwaggerUI.ConfigObject
        {
            PersistAuthorization = true,
            DeepLinking = true
        };
    });
}

app.UseMiddleware<ExceptionMiddleware>();
app.UseDeprecationHeaders();
app.UseHttpsRedirection();
app.UseCors("AllowAngularDev");
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.UseHangfireDashboard("/hangfire");

app.MapHealthChecks("/health");
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