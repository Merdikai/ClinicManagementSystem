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
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapGet("/", () => Results.Ok("Clinic Management API is running"));
app.MapControllers();

app.Run();