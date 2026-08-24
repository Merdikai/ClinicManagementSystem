using System.Security.Cryptography;
using System.Text;
using ClinicManagementSystem.Domain.Entities;
using ClinicManagementSystem.Domain.Enums;
using ClinicManagementSystem.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Infrastructure.SeedData;

public static class DbInitializer
{
    public static async Task SeedAsync(ClinicDbContext context)
    {
        // 1. Seed Roles if missing
        var roleNames = new[] { "Admin", "Doctor", "Nurse", "Receptionist", "Pharmacist", "Accountant", "Patient" };
        foreach (var roleName in roleNames)
        {
            if (!await context.Roles.AnyAsync(r => r.Name == roleName))
            {
                context.Roles.Add(new Role { Name = roleName, Description = $"{roleName} role" });
            }
        }
        await context.SaveChangesAsync();

        var rolesMap = await context.Roles.ToDictionaryAsync(r => r.Name, r => r);

        // 2. Ensure Default Users for all Roles
        var defaultUsers = new[]
        {
            new { Username = "admin", Email = "admin@clinic.com", First = "System", Last = "Administrator", Role = "Admin", Pass = "Admin@123456!", Phone = "+15550000000" },
            new { Username = "dr.smith", Email = "dr.smith@clinic.com", First = "John", Last = "Smith", Role = "Doctor", Pass = "Doctor@12345!", Phone = "+15551112222" },
            new { Username = "nurse.joy", Email = "nurse.joy@clinic.com", First = "Joy", Last = "Nurse", Role = "Nurse", Pass = "Nurse@123456!", Phone = "+15552223333" },
            new { Username = "receptionist.clara", Email = "clara.reception@clinic.com", First = "Clara", Last = "Receptionist", Role = "Receptionist", Pass = "Receptionist@12!", Phone = "+15553334444" },
            new { Username = "pharmacist.sam", Email = "sam.pharma@clinic.com", First = "Sam", Last = "Pharmacist", Role = "Pharmacist", Pass = "Pharmacist@1!", Phone = "+15554445555" },
            new { Username = "accountant.dave", Email = "dave.finance@clinic.com", First = "Dave", Last = "Accountant", Role = "Accountant", Pass = "Accountant@1!", Phone = "+15555556666" },
            new { Username = "patient.alice", Email = "alice.patient@clinic.com", First = "Alice", Last = "Patient", Role = "Patient", Pass = "Patient@1234!", Phone = "+15556667777" }
        };

        foreach (var def in defaultUsers)
        {
            var user = await context.Users.Include(u => u.UserRoles).FirstOrDefaultAsync(u => u.Username == def.Username);
            var role = rolesMap[def.Role];

            if (user is null)
            {
                user = new User
                {
                    Username = def.Username,
                    Email = def.Email,
                    FirstName = def.First,
                    LastName = def.Last,
                    PhoneNumber = def.Phone,
                    PasswordHash = HashPassword(def.Pass),
                    IsActive = true
                };
                context.Users.Add(user);
                await context.SaveChangesAsync();

                context.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });
                await context.SaveChangesAsync();
            }
            else
            {
                user.PasswordHash = HashPassword(def.Pass);
                user.IsActive = true;
                if (!user.UserRoles.Any(ur => ur.RoleId == role.Id))
                {
                    context.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });
                }
                await context.SaveChangesAsync();
            }
        }

        // 3. Seed Essential Pharmacy Medicines Catalog
        if (!await context.Medicines.AnyAsync())
        {
            var medicines = new[]
            {
                new Medicine { Code = "AMX-500", Name = "Amoxicillin 500mg", Category = "Antibiotics", StockQuantity = 120, UnitPrice = 14.50m, BatchNumber = "BATCH-2026-A1", ExpiryDate = DateTime.UtcNow.AddYears(2) },
                new Medicine { Code = "PAR-500", Name = "Paracetamol 500mg", Category = "Analgesic", StockQuantity = 250, UnitPrice = 5.00m, BatchNumber = "BATCH-2026-P1", ExpiryDate = DateTime.UtcNow.AddYears(3) },
                new Medicine { Code = "IBU-400", Name = "Ibuprofen 400mg", Category = "Anti-inflammatory", StockQuantity = 90, UnitPrice = 8.75m, BatchNumber = "BATCH-2026-I1", ExpiryDate = DateTime.UtcNow.AddYears(1) },
                new Medicine { Code = "AZI-250", Name = "Azithromycin 250mg", Category = "Antibiotics", StockQuantity = 45, UnitPrice = 22.00m, BatchNumber = "BATCH-2026-Z1", ExpiryDate = DateTime.UtcNow.AddYears(2) },
                new Medicine { Code = "OMP-20", Name = "Omeprazole 20mg", Category = "Antacid", StockQuantity = 80, UnitPrice = 11.20m, BatchNumber = "BATCH-2026-O1", ExpiryDate = DateTime.UtcNow.AddYears(2) },
                new Medicine { Code = "MET-500", Name = "Metformin 500mg", Category = "Antidiabetic", StockQuantity = 150, UnitPrice = 9.50m, BatchNumber = "BATCH-2026-M1", ExpiryDate = DateTime.UtcNow.AddYears(3) }
            };
            await context.Medicines.AddRangeAsync(medicines);
            await context.SaveChangesAsync();
        }

        // 4. Seed Clinical Prescriptions in Queue if empty
        if (!await context.Prescriptions.AnyAsync())
        {
            var doctorUser = await context.Users.FirstOrDefaultAsync(u => u.Username == "dr.smith");
            var patient = await context.Patients.FirstOrDefaultAsync();
            var amox = await context.Medicines.FirstOrDefaultAsync(m => m.Code == "AMX-500");
            var para = await context.Medicines.FirstOrDefaultAsync(m => m.Code == "PAR-500");

            if (doctorUser != null && patient != null && amox != null)
            {
                var appt = new Appointment
                {
                    PatientId = patient.Id,
                    DoctorId = doctorUser.Id,
                    ScheduledDateTime = DateTime.UtcNow.AddHours(-2),
                    DurationMinutes = 30,
                    Status = AppointmentStatus.Completed,
                    ReasonForVisit = "Acute Respiratory Infection & Fever"
                };
                context.Appointments.Add(appt);
                await context.SaveChangesAsync();

                var consultation = new Consultation
                {
                    AppointmentId = appt.Id,
                    DoctorId = doctorUser.Id,
                    Diagnosis = "Acute Bacterial Upper Respiratory Tract Infection",
                    Symptoms = "Fever, sore throat, cough, nasal congestion",
                    ClinicalNotes = "Oral antibiotic therapy and antipyretics prescribed. Complete full 7-day course.",
                    ConsultedAt = DateTime.UtcNow.AddHours(-1)
                };
                context.Consultations.Add(consultation);
                await context.SaveChangesAsync();

                var prescription = new Prescription
                {
                    ConsultationId = consultation.Id,
                    Notes = "Take full 7-day course. Do not stop early. Take with water after meals.",
                    IssuedAt = DateTime.UtcNow.AddMinutes(-30)
                };
                prescription.PrescriptionItems.Add(new PrescriptionItem
                {
                    MedicineId = amox.Id,
                    Quantity = 2,
                    UnitPrice = amox.UnitPrice,
                    DosageInstructions = "1 capsule (500mg) every 8 hours with water"
                });

                if (para != null)
                {
                    prescription.PrescriptionItems.Add(new PrescriptionItem
                    {
                        MedicineId = para.Id,
                        Quantity = 1,
                        UnitPrice = para.UnitPrice,
                        DosageInstructions = "1 tablet as needed for temperature > 38.5C"
                    });
                }

                context.Prescriptions.Add(prescription);
                await context.SaveChangesAsync();
            }
        }
    }

    private static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }
}
