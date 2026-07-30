using ClinicManagementSystem.Application;
using ClinicManagementSystem.Infrastructure;
using ClinicManagementSystem.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using ClinicManagementSystem.API.Middlewares;
using ClinicManagementSystem.Infrastructure.SeedData;

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

// ─── Controllers & Swagger ───
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// ─── Seed Database ───
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ClinicDbContext>();
    await DbInitializer.SeedAsync(context);
}

// ─── Middleware Pipeline ───
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionMiddleware>();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();