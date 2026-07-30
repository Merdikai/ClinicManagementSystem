using ClinicManagementSystem.Domain.Entities;
using ClinicManagementSystem.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Infrastructure.SeedData;

public static class DbInitializer
{
    public static async Task SeedAsync(ClinicDbContext context)
    {
        // Only seed if roles table is empty
        if (await context.Roles.AnyAsync()) return;

        var roles = new List<Role>
        {
            new() { Name = "Admin", Description = "System administrator with full access" },
            new() { Name = "Doctor", Description = "Medical doctor providing consultations" },
            new() { Name = "Nurse", Description = "Nurse recording vitals and assisting doctors" },
            new() { Name = "Receptionist", Description = "Front desk managing appointments and check-ins" },
            new() { Name = "Pharmacist", Description = "Pharmacy staff dispensing medicines" },
            new() { Name = "Accountant", Description = "Handles billing and payments" },
            new() { Name = "Patient", Description = "Patient with self-service portal access" }
        };

        context.Roles.AddRange(roles);
        await context.SaveChangesAsync();
    }
}