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

// ─── Controllers & Swagger ───
builder.Services.AddControllers(options =>
{
    options.Filters.Add<ValidationFilter>();
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

app.MapGet("/", () => Results.Ok("Clinic Management API is running"));
app.MapControllers();

app.Run();