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
        await SeedLabTestTemplatesAsync(context);
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

    private static async Task SeedLabTestTemplatesAsync(ClinicDbContext context)
    {
        if (await context.LabTestTemplates.AnyAsync()) return;

        var templates = new List<LabTestTemplate>
        {
            new LabTestTemplate
            {
                TestCode = "CBC",
                TestName = "Complete Blood Count (CBC) with Differential",
                Category = "Hematology",
                Description = "Measures overall health and detects wide range of disorders including anemia and leukemia",
                SampleType = "Whole Blood (EDTA)",
                TurnaroundTimeHours = 4,
                Price = 35.00m,
                IsActive = true,
                ParametersJson = @"[
                    {""name"": ""Hemoglobin"", ""unit"": ""g/dL"", ""minRef"": 13.5, ""maxRef"": 17.5, ""normalText"": ""13.5 - 17.5 g/dL""},
                    {""name"": ""WBC Count"", ""unit"": ""10^3/uL"", ""minRef"": 4.5, ""maxRef"": 11.0, ""normalText"": ""4.5 - 11.0 10^3/uL""},
                    {""name"": ""RBC Count"", ""unit"": ""10^6/uL"", ""minRef"": 4.3, ""maxRef"": 5.9, ""normalText"": ""4.3 - 5.9 10^6/uL""},
                    {""name"": ""Platelet Count"", ""unit"": ""10^3/uL"", ""minRef"": 150.0, ""maxRef"": 450.0, ""normalText"": ""150 - 450 10^3/uL""},
                    {""name"": ""Hematocrit (Hct)"", ""unit"": ""%"", ""minRef"": 41.0, ""maxRef"": 50.0, ""normalText"": ""41.0 - 50.0 %""}
                ]"
            },
            new LabTestTemplate
            {
                TestCode = "FBG",
                TestName = "Fasting Blood Glucose",
                Category = "Biochemistry",
                Description = "Evaluates blood glucose levels after an overnight fast to screen for diabetes mellitus",
                SampleType = "Serum / Plasma",
                TurnaroundTimeHours = 2,
                Price = 20.00m,
                IsActive = true,
                ParametersJson = @"[
                    {""name"": ""Fasting Blood Sugar"", ""unit"": ""mg/dL"", ""minRef"": 70.0, ""maxRef"": 99.0, ""normalText"": ""70 - 99 mg/dL""}
                ]"
            },
            new LabTestTemplate
            {
                TestCode = "LIPID",
                TestName = "Comprehensive Lipid Profile",
                Category = "Biochemistry",
                Description = "Assesses risk of cardiovascular disease by measuring cholesterol fractions and triglycerides",
                SampleType = "Serum",
                TurnaroundTimeHours = 6,
                Price = 45.00m,
                IsActive = true,
                ParametersJson = @"[
                    {""name"": ""Total Cholesterol"", ""unit"": ""mg/dL"", ""minRef"": 125.0, ""maxRef"": 200.0, ""normalText"": ""< 200 mg/dL""},
                    {""name"": ""Triglycerides"", ""unit"": ""mg/dL"", ""minRef"": 50.0, ""maxRef"": 150.0, ""normalText"": ""< 150 mg/dL""},
                    {""name"": ""HDL Cholesterol"", ""unit"": ""mg/dL"", ""minRef"": 40.0, ""maxRef"": 90.0, ""normalText"": ""> 40 mg/dL""},
                    {""name"": ""LDL Cholesterol"", ""unit"": ""mg/dL"", ""minRef"": 50.0, ""maxRef"": 100.0, ""normalText"": ""< 100 mg/dL""}
                ]"
            },
            new LabTestTemplate
            {
                TestCode = "CMP",
                TestName = "Comprehensive Metabolic Panel (CMP)",
                Category = "Biochemistry",
                Description = "Evaluates organ function, kidney/liver health, electrolyte levels, and fluid balance",
                SampleType = "Serum",
                TurnaroundTimeHours = 6,
                Price = 60.00m,
                IsActive = true,
                ParametersJson = @"[
                    {""name"": ""Sodium (Na)"", ""unit"": ""mEq/L"", ""minRef"": 135.0, ""maxRef"": 145.0, ""normalText"": ""135 - 145 mEq/L""},
                    {""name"": ""Potassium (K)"", ""unit"": ""mEq/L"", ""minRef"": 3.5, ""maxRef"": 5.0, ""normalText"": ""3.5 - 5.0 mEq/L""},
                    {""name"": ""Chloride (Cl)"", ""unit"": ""mEq/L"", ""minRef"": 96.0, ""maxRef"": 106.0, ""normalText"": ""96 - 106 mEq/L""},
                    {""name"": ""Blood Urea Nitrogen (BUN)"", ""unit"": ""mg/dL"", ""minRef"": 7.0, ""maxRef"": 20.0, ""normalText"": ""7 - 20 mg/dL""},
                    {""name"": ""Creatinine"", ""unit"": ""mg/dL"", ""minRef"": 0.6, ""maxRef"": 1.2, ""normalText"": ""0.6 - 1.2 mg/dL""},
                    {""name"": ""Calcium"", ""unit"": ""mg/dL"", ""minRef"": 8.5, ""maxRef"": 10.2, ""normalText"": ""8.5 - 10.2 mg/dL""}
                ]"
            },
            new LabTestTemplate
            {
                TestCode = "URINE",
                TestName = "Routine Urinalysis (Macro & Microscopic)",
                Category = "Urinalysis",
                Description = "Screens for urinary tract infections, kidney disorders, and metabolic conditions",
                SampleType = "Midstream Urine",
                TurnaroundTimeHours = 2,
                Price = 25.00m,
                IsActive = true,
                ParametersJson = @"[
                    {""name"": ""Specific Gravity"", ""unit"": """", ""minRef"": 1.005, ""maxRef"": 1.030, ""normalText"": ""1.005 - 1.030""},
                    {""name"": ""pH"", ""unit"": """", ""minRef"": 4.5, ""maxRef"": 8.0, ""normalText"": ""4.5 - 8.0""},
                    {""name"": ""Protein"", ""unit"": """", ""minRef"": 0, ""maxRef"": 0, ""normalText"": ""Negative""},
                    {""name"": ""Glucose"", ""unit"": """", ""minRef"": 0, ""maxRef"": 0, ""normalText"": ""Negative""},
                    {""name"": ""Ketones"", ""unit"": """", ""minRef"": 0, ""maxRef"": 0, ""normalText"": ""Negative""},
                    {""name"": ""Leukocyte Esterase"", ""unit"": """", ""minRef"": 0, ""maxRef"": 0, ""normalText"": ""Negative""}
                ]"
            },
            new LabTestTemplate
            {
                TestCode = "LFT",
                TestName = "Liver Function Panel (LFT)",
                Category = "Biochemistry",
                Description = "Screens for liver damage, hepatitis, and tracks response to medication therapies",
                SampleType = "Serum",
                TurnaroundTimeHours = 4,
                Price = 50.00m,
                IsActive = true,
                ParametersJson = @"[
                    {""name"": ""ALT (Alanine Transaminase)"", ""unit"": ""U/L"", ""minRef"": 7.0, ""maxRef"": 56.0, ""normalText"": ""7 - 56 U/L""},
                    {""name"": ""AST (Aspartate Transaminase)"", ""unit"": ""U/L"", ""minRef"": 10.0, ""maxRef"": 40.0, ""normalText"": ""10 - 40 U/L""},
                    {""name"": ""ALP (Alkaline Phosphatase)"", ""unit"": ""U/L"", ""minRef"": 44.0, ""maxRef"": 147.0, ""normalText"": ""44 - 147 U/L""},
                    {""name"": ""Total Bilirubin"", ""unit"": ""mg/dL"", ""minRef"": 0.1, ""maxRef"": 1.2, ""normalText"": ""0.1 - 1.2 mg/dL""},
                    {""name"": ""Albumin"", ""unit"": ""g/dL"", ""minRef"": 3.5, ""maxRef"": 5.0, ""normalText"": ""3.5 - 5.0 g/dL""}
                ]"
            },
            new LabTestTemplate
            {
                TestCode = "TSH",
                TestName = "Thyroid Stimulating Hormone (TSH) & Free T4",
                Category = "Endocrinology",
                Description = "Assesses thyroid gland function to detect hypothyroidism or hyperthyroidism",
                SampleType = "Serum",
                TurnaroundTimeHours = 12,
                Price = 40.00m,
                IsActive = true,
                ParametersJson = @"[
                    {""name"": ""TSH"", ""unit"": ""uIU/mL"", ""minRef"": 0.4, ""maxRef"": 4.0, ""normalText"": ""0.4 - 4.0 uIU/mL""},
                    {""name"": ""Free T4"", ""unit"": ""ng/dL"", ""minRef"": 0.8, ""maxRef"": 1.8, ""normalText"": ""0.8 - 1.8 ng/dL""}
                ]"
            }
        };

        context.LabTestTemplates.AddRange(templates);
        await context.SaveChangesAsync();
    }
}


