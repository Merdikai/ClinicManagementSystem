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
        await SeedMedicinesAsync(context);
        
        // 1. Seed Roles if missing
        var roleNames = new[] { "Admin", "Doctor", "Nurse", "Receptionist", "Pharmacist", "Accountant", "Patient", "LabTechnician", "Radiologist" };
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
            new { Username = "dr.james", Email = "dr.james@clinic.com", First = "Sarah", Last = "James", Role = "Doctor", Pass = "Doctor@12345!", Phone = "+15551113333" },
            new { Username = "nurse.joy", Email = "nurse.joy@clinic.com", First = "Joy", Last = "Nurse", Role = "Nurse", Pass = "Nurse@123456!", Phone = "+15552223333" },
            new { Username = "receptionist.clara", Email = "clara.reception@clinic.com", First = "Clara", Last = "Receptionist", Role = "Receptionist", Pass = "Receptionist@12!", Phone = "+15553334444" },
            new { Username = "pharmacist.sam", Email = "sam.pharma@clinic.com", First = "Sam", Last = "Pharmacist", Role = "Pharmacist", Pass = "Pharmacist@1!", Phone = "+15554445555" },
            new { Username = "accountant.dave", Email = "dave.finance@clinic.com", First = "Dave", Last = "Accountant", Role = "Accountant", Pass = "Accountant@1!", Phone = "+15555556666" },
            new { Username = "patient.alice", Email = "alice.patient@clinic.com", First = "Alice", Last = "Patient", Role = "Patient", Pass = "Patient@1234!", Phone = "+15556667777" },
            new { Username = "lab.tech", Email = "lab.tech@clinic.com", First = "Leo", Last = "Technician", Role = "LabTechnician", Pass = "LabTech@123!", Phone = "+15557778888" },
            new { Username = "radiologist.ray", Email = "radiologist.ray@clinic.com", First = "Ray", Last = "Radiologist", Role = "Radiologist", Pass = "Radiology@123!", Phone = "+15558889999" },
            new { Username = "radiologist.elena", Email = "radiologist.elena@clinic.com", First = "Elena", Last = "Vance", Role = "Radiologist", Pass = "Radiology@123!", Phone = "+15558889988" }
        };

        foreach (var def in defaultUsers)
        {
            var user = await context.Users.Include(u => u.UserRoles).FirstOrDefaultAsync(u => u.Username == def.Username || u.Email == def.Email);
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
                    await context.SaveChangesAsync();
                }
            }
        }

        // 3. Seed Role Permissions
        await SeedRolePermissionsAsync(context);
        await SeedDoctorSchedulesAsync(context);
        await SeedHipaaAuditLogsAsync(context);
        await SeedAlicePatientAndRecordsAsync(context);
        await SeedFinanceDataAsync(context);
        await SeedLaboratorySuiteAsync(context);
        await SeedRadiologySuiteAsync(context);
        await SeedAllPatientComprehensiveRecordsAsync(context);
        await SeedMultiDoctorAppointmentsAsync(context);
    }

    private static async Task SeedRolePermissionsAsync(ClinicDbContext context)
    {
        var roles = await context.Roles.ToListAsync();
        var adminRole = roles.FirstOrDefault(r => r.Name == "Admin");
        var docRole = roles.FirstOrDefault(r => r.Name == "Doctor");
        var nurseRole = roles.FirstOrDefault(r => r.Name == "Nurse");
        var pharmaRole = roles.FirstOrDefault(r => r.Name == "Pharmacist");
        var recepRole = roles.FirstOrDefault(r => r.Name == "Receptionist");
        var accRole = roles.FirstOrDefault(r => r.Name == "Accountant");
        var labRole = roles.FirstOrDefault(r => r.Name == "LabTechnician");

        var permMap = new Dictionary<Guid, string[]>();

        if (adminRole != null) permMap[adminRole.Id] = new[]
        {
            "Patients.View", "Patients.Export",
            "Billing.Reports", "Audit.View",
            "StaffDuty.ClockInOut",
            "ClinicalGovernance.DelegateAuthority", "Governance.DelegateAuthority",
            "UserManagement.View", "UserManagement.ManageRoles", "UserManagement.AuditLogs", "Users.Manage"
        };
        if (docRole != null) permMap[docRole.Id] = new[]
        {
            "Patients.View", "Patients.Edit", "Patients.Export", "Emr.View", "Emr.BreakGlass",
            "Consultations.View", "Consultations.Create", "Consultations.Edit", "Consultations.Sign",
            "Appointments.View", "Appointments.Book", "Appointments.Manage",
            "Prescriptions.View", "Prescriptions.Create", "Prescriptions.Issue",
            "LabOrders.View", "LabOrders.Create", "Lab.Order",
            "Radiology.View", "Radiology.Order", "Radiology.SignOff",
            "StaffDuty.ClockInOut", "ClinicalGovernance.BreakGlass", "ClinicalGovernance.DelegateAuthority"
        };
        if (nurseRole != null) permMap[nurseRole.Id] = new[]
        {
            "Patients.View", "Patients.Create", "Patients.Edit", "Emr.View",
            "Consultations.View",
            "Appointments.View", "Appointments.Book", "Appointments.CheckIn", "Appointments.Triage",
            "Prescriptions.View",
            "LabOrders.View", "LabOrders.Process", "Lab.Collect", "Lab.Analyze",
            "Radiology.View",
            "VitalSigns.Record", "VitalSigns.View",
            "NursingProcedures.Perform", "NursingProcedures.View",
            "ShiftHandover.Create", "ShiftHandover.View",
            "StaffDuty.ClockInOut", "ClinicalGovernance.BreakGlass"
        };
        if (pharmaRole != null) permMap[pharmaRole.Id] = new[]
        {
            "Patients.View",
            "Prescriptions.View", "Prescriptions.Dispense", "Pharmacy.ManageInventory",
            "Medicines.View", "Medicines.Manage",
            "Billing.View",
            "StaffDuty.ClockInOut"
        };
        if (recepRole != null) permMap[recepRole.Id] = new[]
        {
            "Patients.View", "Patients.Create", "Patients.Edit",
            "Appointments.View", "Appointments.Book", "Appointments.Cancel", "Appointments.CheckIn", "Appointments.Manage",
            "Billing.View", "Billing.CreateInvoice", "Billing.ProcessPayment", "Billing.CollectPayment",
            "StaffDuty.ClockInOut"
        };
        if (accRole != null) permMap[accRole.Id] = new[]
        {
            "Patients.View",
            "Billing.View", "Billing.CreateInvoice", "Billing.ProcessPayment", "Billing.Reports",
            "Billing.ApplyDiscount", "Billing.CollectPayment", "Billing.Claims", "Billing.Adjustments", "Billing.Expenses", "Billing.PettyCash", "Billing.Payroll", "Billing.Reconciliation",
            "Audit.View", "StaffDuty.ClockInOut"
        };
        if (labRole != null) permMap[labRole.Id] = new[]
        {
            "Patients.View",
            "LabOrders.View", "LabOrders.Process", "LabOrders.Verify",
            "Lab.Analyze", "Lab.VerifyResults", "Lab.Inventory", "Lab.Equipment",
            "Billing.View",
            "StaffDuty.ClockInOut"
        };
        var radRole = roles.FirstOrDefault(r => r.Name == "Radiologist");
        if (radRole != null) permMap[radRole.Id] = new[]
        {
            "Patients.View",
            "Radiology.View", "Radiology.Order", "Radiology.Report", "Radiology.SignOff", "Radiology.SafetyDose", "Radiology.Suites", "Radiology.ExecuteScan", "Radiology.AcknowledgeAlert",
            "Appointments.View",
            "StaffDuty.ClockInOut"
        };
        var patientRole = roles.FirstOrDefault(r => r.Name == "Patient");
        if (patientRole != null) permMap[patientRole.Id] = new[]
        {
            "Appointments.View", "Appointments.Book", "Appointments.Cancel", "Appointments.Manage",
            "Consultations.View",
            "Prescriptions.View",
            "LabOrders.View",
            "Radiology.View",
            "Billing.View"
        };

        foreach (var kvp in permMap)
        {
            var existing = await context.RolePermissions
                .Where(rp => rp.RoleId == kvp.Key)
                .ToListAsync();

            context.RolePermissions.RemoveRange(existing);

            foreach (var p in kvp.Value)
            {
                context.RolePermissions.Add(new RolePermission
                {
                    Id = Guid.NewGuid(),
                    RoleId = kvp.Key,
                    Permission = p,
                    GrantedAt = DateTime.UtcNow
                });
            }
        }
        await context.SaveChangesAsync();
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
                Description = "Comprehensive screen for anemia, infection, and various blood disorders",
                SampleType = "Whole Blood (EDTA)",
                TurnaroundTimeHours = 2,
                Price = 25.00m,
                IsActive = true,
                ParametersJson = @"[
                    {""name"": ""WBC Count"", ""unit"": ""10^3/uL"", ""minRef"": 4.5, ""maxRef"": 11.0, ""normalText"": ""4.5 - 11.0 10^3/uL""},
                    {""name"": ""RBC Count"", ""unit"": ""10^6/uL"", ""minRef"": 4.0, ""maxRef"": 5.9, ""normalText"": ""4.0 - 5.9 10^6/uL""},
                    {""name"": ""Hemoglobin"", ""unit"": ""g/dL"", ""minRef"": 13.5, ""maxRef"": 17.5, ""normalText"": ""13.5 - 17.5 g/dL""},
                    {""name"": ""Hematocrit"", ""unit"": ""%"", ""minRef"": 38.0, ""maxRef"": 50.0, ""normalText"": ""38.0 - 50.0%""},
                    {""name"": ""Platelet Count"", ""unit"": ""10^3/uL"", ""minRef"": 150.0, ""maxRef"": 450.0, ""normalText"": ""150 - 450 10^3/uL""}
                ]"
            },
            new LabTestTemplate
            {
                TestCode = "FBG",
                TestName = "Fasting Blood Glucose",
                Category = "Biochemistry",
                Description = "Standard blood glucose test after 8-12 hours fasting to evaluate diabetes risk",
                SampleType = "Fluoride Oxalate Plasma",
                TurnaroundTimeHours = 1,
                Price = 15.00m,
                IsActive = true,
                ParametersJson = @"[
                    {""name"": ""Fasting Glucose"", ""unit"": ""mg/dL"", ""minRef"": 70.0, ""maxRef"": 99.0, ""normalText"": ""70 - 99 mg/dL (Normal)""}
                ]"
            },
            new LabTestTemplate
            {
                TestCode = "LIPID",
                TestName = "Comprehensive Lipid Profile",
                Category = "Lipid",
                Description = "Total cholesterol, HDL, LDL, and triglycerides for cardiovascular risk assessment",
                SampleType = "Serum",
                TurnaroundTimeHours = 4,
                Price = 35.00m,
                IsActive = true,
                ParametersJson = @"[
                    {""name"": ""Total Cholesterol"", ""unit"": ""mg/dL"", ""minRef"": 100.0, ""maxRef"": 199.0, ""normalText"": ""< 200 mg/dL (Desirable)""},
                    {""name"": ""HDL Cholesterol"", ""unit"": ""mg/dL"", ""minRef"": 40.0, ""maxRef"": 80.0, ""normalText"": ""> 40 mg/dL""},
                    {""name"": ""LDL Cholesterol"", ""unit"": ""mg/dL"", ""minRef"": 50.0, ""maxRef"": 100.0, ""normalText"": ""< 100 mg/dL (Optimal)""},
                    {""name"": ""Triglycerides"", ""unit"": ""mg/dL"", ""minRef"": 50.0, ""maxRef"": 150.0, ""normalText"": ""< 150 mg/dL (Normal)""}
                ]"
            },
            new LabTestTemplate
            {
                TestCode = "CMP",
                TestName = "Comprehensive Metabolic Panel (CMP)",
                Category = "Biochemistry",
                Description = "Evaluates organ function, kidney/liver status, electrolyte and fluid balance",
                SampleType = "Serum",
                TurnaroundTimeHours = 4,
                Price = 45.00m,
                IsActive = true,
                ParametersJson = @"[
                    {""name"": ""Sodium"", ""unit"": ""mmol/L"", ""minRef"": 135.0, ""maxRef"": 145.0, ""normalText"": ""135 - 145 mmol/L""},
                    {""name"": ""Potassium"", ""unit"": ""mmol/L"", ""minRef"": 3.5, ""maxRef"": 5.0, ""normalText"": ""3.5 - 5.0 mmol/L""},
                    {""name"": ""Chloride"", ""unit"": ""mmol/L"", ""minRef"": 96.0, ""maxRef"": 106.0, ""normalText"": ""96 - 106 mmol/L""},
                    {""name"": ""Carbon Dioxide (CO2)"", ""unit"": ""mmol/L"", ""minRef"": 22.0, ""maxRef"": 29.0, ""normalText"": ""22 - 29 mmol/L""},
                    {""name"": ""Blood Urea Nitrogen (BUN)"", ""unit"": ""mg/dL"", ""minRef"": 7.0, ""maxRef"": 20.0, ""normalText"": ""7 - 20 mg/dL""},
                    {""name"": ""Creatinine"", ""unit"": ""mg/dL"", ""minRef"": 0.7, ""maxRef"": 1.3, ""normalText"": ""0.7 - 1.3 mg/dL""},
                    {""name"": ""Calcium"", ""unit"": ""mg/dL"", ""minRef"": 8.6, ""maxRef"": 10.2, ""normalText"": ""8.6 - 10.2 mg/dL""}
                ]"
            },
            new LabTestTemplate
            {
                TestCode = "URINE",
                TestName = "Urinalysis Complete with Microscopic",
                Category = "Urinalysis",
                Description = "Detects urinary tract infections, kidney disease, and diabetes indicators",
                SampleType = "Clean Catch Midstream Urine",
                TurnaroundTimeHours = 2,
                Price = 20.00m,
                IsActive = true,
                ParametersJson = @"[
                    {""name"": ""Color & Appearance"", ""unit"": ""text"", ""normalText"": ""Pale Yellow, Clear""},
                    {""name"": ""Specific Gravity"", ""unit"": ""ratio"", ""minRef"": 1.005, ""maxRef"": 1.030, ""normalText"": ""1.005 - 1.030""},
                    {""name"": ""pH"", ""unit"": ""pH"", ""minRef"": 5.0, ""maxRef"": 8.0, ""normalText"": ""5.0 - 8.0""},
                    {""name"": ""Protein"", ""unit"": ""mg/dL"", ""normalText"": ""Negative""},
                    {""name"": ""Glucose"", ""unit"": ""mg/dL"", ""normalText"": ""Negative""},
                    {""name"": ""Leukocyte Esterase"", ""unit"": ""text"", ""normalText"": ""Negative""},
                    {""name"": ""Nitrite"", ""unit"": ""text"", ""normalText"": ""Negative""}
                ]"
            },
            new LabTestTemplate
            {
                TestCode = "LFT",
                TestName = "Liver Function Panel (LFT)",
                Category = "Hepatic",
                Description = "Measures enzymes, proteins, and bilirubin substances produced by the liver",
                SampleType = "Serum",
                TurnaroundTimeHours = 4,
                Price = 38.00m,
                IsActive = true,
                ParametersJson = @"[
                    {""name"": ""ALT (Alanine Aminotransferase)"", ""unit"": ""U/L"", ""minRef"": 7.0, ""maxRef"": 56.0, ""normalText"": ""7 - 56 U/L""},
                    {""name"": ""AST (Aspartate Aminotransferase)"", ""unit"": ""U/L"", ""minRef"": 10.0, ""maxRef"": 40.0, ""normalText"": ""10 - 40 U/L""},
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

    private static string HashPassword(string password)
    {
        return ClinicManagementSystem.Infrastructure.Identity.PasswordHasher.HashPassword(password);
    }

    private static async Task SeedDoctorSchedulesAsync(ClinicDbContext context)
    {
        if (await context.DoctorSchedules.AnyAsync())
            return;

        var doctorSmith = await context.Users.FirstOrDefaultAsync(u => u.Username == "dr.smith");
        var doctorJames = await context.Users.FirstOrDefaultAsync(u => u.Username == "dr.james");

        var schedules = new List<DoctorSchedule>();

        if (doctorSmith != null)
        {
            schedules.AddRange(new[]
            {
                new DoctorSchedule { Id = Guid.NewGuid(), DoctorId = doctorSmith.Id, DayOfWeek = DayOfWeek.Monday, StartTime = new TimeSpan(9, 0, 0), EndTime = new TimeSpan(17, 0, 0), SlotDurationMinutes = 30, IsActive = true },
                new DoctorSchedule { Id = Guid.NewGuid(), DoctorId = doctorSmith.Id, DayOfWeek = DayOfWeek.Tuesday, StartTime = new TimeSpan(9, 0, 0), EndTime = new TimeSpan(17, 0, 0), SlotDurationMinutes = 30, IsActive = true },
                new DoctorSchedule { Id = Guid.NewGuid(), DoctorId = doctorSmith.Id, DayOfWeek = DayOfWeek.Wednesday, StartTime = new TimeSpan(9, 0, 0), EndTime = new TimeSpan(13, 0, 0), SlotDurationMinutes = 30, IsActive = true },
                new DoctorSchedule { Id = Guid.NewGuid(), DoctorId = doctorSmith.Id, DayOfWeek = DayOfWeek.Thursday, StartTime = new TimeSpan(9, 0, 0), EndTime = new TimeSpan(17, 0, 0), SlotDurationMinutes = 30, IsActive = true },
                new DoctorSchedule { Id = Guid.NewGuid(), DoctorId = doctorSmith.Id, DayOfWeek = DayOfWeek.Friday, StartTime = new TimeSpan(10, 0, 0), EndTime = new TimeSpan(16, 0, 0), SlotDurationMinutes = 30, IsActive = true }
            });
        }

        if (doctorJames != null)
        {
            schedules.AddRange(new[]
            {
                new DoctorSchedule { Id = Guid.NewGuid(), DoctorId = doctorJames.Id, DayOfWeek = DayOfWeek.Monday, StartTime = new TimeSpan(13, 0, 0), EndTime = new TimeSpan(20, 0, 0), SlotDurationMinutes = 30, IsActive = true },
                new DoctorSchedule { Id = Guid.NewGuid(), DoctorId = doctorJames.Id, DayOfWeek = DayOfWeek.Wednesday, StartTime = new TimeSpan(13, 0, 0), EndTime = new TimeSpan(20, 0, 0), SlotDurationMinutes = 30, IsActive = true },
                new DoctorSchedule { Id = Guid.NewGuid(), DoctorId = doctorJames.Id, DayOfWeek = DayOfWeek.Friday, StartTime = new TimeSpan(13, 0, 0), EndTime = new TimeSpan(20, 0, 0), SlotDurationMinutes = 30, IsActive = true },
                new DoctorSchedule { Id = Guid.NewGuid(), DoctorId = doctorJames.Id, DayOfWeek = DayOfWeek.Saturday, StartTime = new TimeSpan(9, 0, 0), EndTime = new TimeSpan(15, 0, 0), SlotDurationMinutes = 30, IsActive = true }
            });
        }

        var radRay = await context.Users.FirstOrDefaultAsync(u => u.Username == "radiologist.ray");
        if (radRay != null)
        {
            schedules.AddRange(new[]
            {
                new DoctorSchedule { Id = Guid.NewGuid(), DoctorId = radRay.Id, DayOfWeek = DayOfWeek.Monday, StartTime = new TimeSpan(8, 0, 0), EndTime = new TimeSpan(16, 0, 0), SlotDurationMinutes = 20, IsActive = true },
                new DoctorSchedule { Id = Guid.NewGuid(), DoctorId = radRay.Id, DayOfWeek = DayOfWeek.Tuesday, StartTime = new TimeSpan(8, 0, 0), EndTime = new TimeSpan(16, 0, 0), SlotDurationMinutes = 20, IsActive = true },
                new DoctorSchedule { Id = Guid.NewGuid(), DoctorId = radRay.Id, DayOfWeek = DayOfWeek.Wednesday, StartTime = new TimeSpan(8, 0, 0), EndTime = new TimeSpan(16, 0, 0), SlotDurationMinutes = 20, IsActive = true },
                new DoctorSchedule { Id = Guid.NewGuid(), DoctorId = radRay.Id, DayOfWeek = DayOfWeek.Thursday, StartTime = new TimeSpan(8, 0, 0), EndTime = new TimeSpan(16, 0, 0), SlotDurationMinutes = 20, IsActive = true },
                new DoctorSchedule { Id = Guid.NewGuid(), DoctorId = radRay.Id, DayOfWeek = DayOfWeek.Friday, StartTime = new TimeSpan(8, 0, 0), EndTime = new TimeSpan(14, 0, 0), SlotDurationMinutes = 20, IsActive = true }
            });
        }

        if (schedules.Any())
        {
            await context.DoctorSchedules.AddRangeAsync(schedules);
            await context.SaveChangesAsync();
        }
    }

    private static async Task SeedHipaaAuditLogsAsync(ClinicDbContext context)
    {
        if (await context.EmrAccessAuditLogs.AnyAsync())
            return;

        // Resolve up to 3 real patients for realistic seed records
        var patients = await context.Patients.Take(3).ToListAsync();
        var p1Id = patients.Count > 0 ? patients[0].Id : Guid.NewGuid();
        var p2Id = patients.Count > 1 ? patients[1].Id : p1Id;
        var p3Id = patients.Count > 2 ? patients[2].Id : p1Id;

        var logs = new List<EmrAccessAuditLog>
        {
            // ─── Routine chart views (2 days ago) ────────────────────────────
            new EmrAccessAuditLog
            {
                Id = Guid.NewGuid(), PatientId = p1Id, UserId = Guid.NewGuid(),
                UserName = "dr.smith", UserRole = "Doctor",
                AccessType = "ViewChart",
                Reason = "Routine clinical consultation — annual cardiac review and BP assessment",
                IsBreakGlassEmergency = false, IpAddress = "192.168.1.45",
                Timestamp = DateTime.UtcNow.AddDays(-2).AddHours(-6)
            },
            new EmrAccessAuditLog
            {
                Id = Guid.NewGuid(), PatientId = p2Id, UserId = Guid.NewGuid(),
                UserName = "nurse.joy", UserRole = "Nurse",
                AccessType = "ViewVitals",
                Reason = "Pre-consultation triage — BP, SpO2, temperature, and weight recording",
                IsBreakGlassEmergency = false, IpAddress = "192.168.1.52",
                Timestamp = DateTime.UtcNow.AddDays(-2).AddHours(-5)
            },
            new EmrAccessAuditLog
            {
                Id = Guid.NewGuid(), PatientId = p3Id, UserId = Guid.NewGuid(),
                UserName = "pharmacist.sam", UserRole = "Pharmacist",
                AccessType = "ViewChart",
                Reason = "Prescription review — metformin dosage verification and contraindication check",
                IsBreakGlassEmergency = false, IpAddress = "192.168.1.80",
                Timestamp = DateTime.UtcNow.AddDays(-2).AddHours(-4)
            },
            // ─── Lab & Radiology access ───────────────────────────────────────
            new EmrAccessAuditLog
            {
                Id = Guid.NewGuid(), PatientId = p1Id, UserId = Guid.NewGuid(),
                UserName = "labt.kim", UserRole = "LabTechnician",
                AccessType = "ViewLabResults",
                Reason = "Full blood count result entry and CBC panel verification",
                IsBreakGlassEmergency = false, IpAddress = "192.168.1.90",
                Timestamp = DateTime.UtcNow.AddDays(-1).AddHours(-8)
            },
            new EmrAccessAuditLog
            {
                Id = Guid.NewGuid(), PatientId = p2Id, UserId = Guid.NewGuid(),
                UserName = "rad.jones", UserRole = "Radiologist",
                AccessType = "ViewProfile",
                Reason = "CT chest imaging interpretation and radiology report sign-off",
                IsBreakGlassEmergency = false, IpAddress = "192.168.1.75",
                Timestamp = DateTime.UtcNow.AddDays(-1).AddHours(-5)
            },
            // ─── Consultation history ─────────────────────────────────────────
            new EmrAccessAuditLog
            {
                Id = Guid.NewGuid(), PatientId = p3Id, UserId = Guid.NewGuid(),
                UserName = "dr.smith", UserRole = "Doctor",
                AccessType = "ViewConsultations",
                Reason = "Follow-up consultation review — specialist referral decision for nephrology",
                IsBreakGlassEmergency = false, IpAddress = "192.168.1.45",
                Timestamp = DateTime.UtcNow.AddDays(-1).AddHours(-2)
            },
            // ─── Break-Glass emergency events ─────────────────────────────────
            new EmrAccessAuditLog
            {
                Id = Guid.NewGuid(), PatientId = p1Id, UserId = Guid.NewGuid(),
                UserName = "dr.smith", UserRole = "Doctor",
                AccessType = "BreakGlassEmergencyOverride",
                Reason = "STEMI suspected — critical chest pain, ECG changes. Emergency Break-Glass for immediate medication history and allergy access.",
                IsBreakGlassEmergency = true, IpAddress = "192.168.1.45",
                Timestamp = DateTime.UtcNow.AddHours(-10)
            },
            new EmrAccessAuditLog
            {
                Id = Guid.NewGuid(), PatientId = p2Id, UserId = Guid.NewGuid(),
                UserName = "nurse.joy", UserRole = "Nurse",
                AccessType = "BreakGlassEmergencyOverride",
                Reason = "Patient unresponsive post-procedure — Break-Glass override for allergy and medication history to support emergency resuscitation protocol.",
                IsBreakGlassEmergency = true, IpAddress = "192.168.1.52",
                Timestamp = DateTime.UtcNow.AddHours(-4)
            },
            // ─── Recent standard activity ─────────────────────────────────────
            new EmrAccessAuditLog
            {
                Id = Guid.NewGuid(), PatientId = p1Id, UserId = Guid.NewGuid(),
                UserName = "nurse.joy", UserRole = "Nurse",
                AccessType = "ViewChart",
                Reason = "Medication administration check — IV drip rate verification before administration",
                IsBreakGlassEmergency = false, IpAddress = "192.168.1.52",
                Timestamp = DateTime.UtcNow.AddHours(-2)
            },
            new EmrAccessAuditLog
            {
                Id = Guid.NewGuid(), PatientId = p3Id, UserId = Guid.NewGuid(),
                UserName = "dr.smith", UserRole = "Doctor",
                AccessType = "ViewConsultations",
                Reason = "Post-surgical follow-up — wound assessment and anaesthesia recovery notes review",
                IsBreakGlassEmergency = false, IpAddress = "192.168.1.45",
                Timestamp = DateTime.UtcNow.AddMinutes(-45)
            }
        };

        await context.EmrAccessAuditLogs.AddRangeAsync(logs);
        await context.SaveChangesAsync();
    }

    private static async Task SeedAlicePatientAndRecordsAsync(ClinicDbContext context)
    {
        var aliceId = Guid.Parse("a11ce000-0000-0000-0000-000000000001");
        var existingAlice = await context.Patients.FirstOrDefaultAsync(p => p.Id == aliceId || p.Email == "alice.patient@clinic.com");
        
        if (existingAlice == null)
        {
            existingAlice = new Patient
            {
                Id = aliceId,
                MedicalRecordNumber = "PAT-ALICE-001",
                FirstName = "Alice",
                LastName = "Patient",
                DateOfBirth = new DateTime(1992, 4, 15, 0, 0, 0, DateTimeKind.Utc),
                Gender = "Female",
                Phone = "+1 (555) 666-7777",
                Email = "alice.patient@clinic.com",
                Address = "742 Evergreen Terrace, Springfield",
                BloodGroup = "O+",
                EmergencyContact = "Bob Patient (+1 555-999-8888)",
                RegisteredAt = DateTime.UtcNow.AddMonths(-6)
            };
            context.Patients.Add(existingAlice);
            await context.SaveChangesAsync();
        }

        var doctor = await context.Users.FirstOrDefaultAsync(u => u.UserRoles.Any(ur => ur.Role.Name == "Doctor"));
        var doctorId = doctor?.Id ?? Guid.NewGuid();
        var nurse = await context.Users.FirstOrDefaultAsync(u => u.UserRoles.Any(ur => ur.Role.Name == "Nurse"));
        var nurseId = nurse?.Id ?? doctorId;

        // Check if Alice has appointments
        var hasAppointments = await context.Appointments.AnyAsync(a => a.PatientId == existingAlice.Id);
        if (!hasAppointments)
        {
            // 1. Completed Consultation Appointment
            var apptPast = new Appointment
            {
                Id = Guid.NewGuid(),
                PatientId = existingAlice.Id,
                DoctorId = doctorId,
                ScheduledDateTime = DateTime.UtcNow.AddDays(-14),
                DurationMinutes = 30,
                Status = AppointmentStatus.Completed,
                ReasonForVisit = "Seasonal allergy assessment and routine health check"
            };

            var vitals = new VitalSign
            {
                Id = Guid.NewGuid(),
                AppointmentId = apptPast.Id,
                RecordedByUserId = nurseId,
                SystolicBP = 118,
                DiastolicBP = 76,
                HeartRateBpm = 72,
                TemperatureC = 36.6m,
                RespiratoryRate = 16,
                WeightKg = 64.0m,
                HeightCm = 168.0m,
                RecordedAt = DateTime.UtcNow.AddDays(-14)
            };

            var consult = new Consultation
            {
                Id = Guid.NewGuid(),
                AppointmentId = apptPast.Id,
                DoctorId = doctorId,
                Symptoms = "Nasal congestion, sneezing, mild eye redness for 2 weeks",
                Diagnosis = "Seasonal Allergic Rhinitis (Pollinosis)",
                ClinicalNotes = "Patient is in good overall health. Heart and lungs clear. Nasal mucosa mildly edematous. Recommended oral antihistamine and nasal spray. Follow up as needed.",
                ConsultedAt = DateTime.UtcNow.AddDays(-14)
            };

            var medicine = await context.Medicines.FirstOrDefaultAsync(m => m.Name.Contains("Cetirizine")) ?? await context.Medicines.FirstOrDefaultAsync();
            var prescription = new Prescription
            {
                Id = Guid.NewGuid(),
                ConsultationId = consult.Id,
                Notes = "Take medication after food. Avoid known outdoor pollen peaks.",
                Status = "Pending",
                IssuedAt = DateTime.UtcNow.AddDays(-1)
            };

            if (medicine != null)
            {
                prescription.PrescriptionItems.Add(new PrescriptionItem
                {
                    Id = Guid.NewGuid(),
                    PrescriptionId = prescription.Id,
                    MedicineId = medicine.Id,
                    Quantity = 2,
                    DosageInstructions = "1 tablet once daily in the evening for 14 days",
                    UnitPrice = medicine.UnitPrice,
                    Status = "Pending",
                    DispensedQuantity = 0
                });
            }


            // 2. Upcoming Scheduled Appointment
            var apptUpcoming = new Appointment
            {
                Id = Guid.NewGuid(),
                PatientId = existingAlice.Id,
                DoctorId = doctorId,
                ScheduledDateTime = DateTime.UtcNow.AddDays(4).Date.AddHours(10),
                DurationMinutes = 30,
                Status = AppointmentStatus.Scheduled,
                ReasonForVisit = "Follow-up consultation & allergy symptom review"
            };

            // 3. Pending Approval Appointment
            var apptPending = new Appointment
            {
                Id = Guid.NewGuid(),
                PatientId = existingAlice.Id,
                DoctorId = doctorId,
                ScheduledDateTime = DateTime.UtcNow.AddDays(12).Date.AddHours(14),
                DurationMinutes = 30,
                Status = AppointmentStatus.PendingApproval,
                ReasonForVisit = "Comprehensive blood screening & vitamin panel review"
            };

            // 4. Invoices for Alice
            var invoice = new Invoice
            {
                Id = Guid.NewGuid(),
                InvoiceNumber = "INV-ALICE-1001",
                PatientId = existingAlice.Id,
                AppointmentId = apptPast.Id,
                IssueDate = DateTime.UtcNow.AddDays(-14),
                DueDate = DateTime.UtcNow.AddDays(16),
                SubTotal = 120.00m,
                TaxAmount = 0.00m,
                DiscountAmount = 0.00m,
                TotalAmount = 120.00m,
                Status = InvoiceStatus.Paid
            };

            invoice.LineItems.Add(new InvoiceItem
            {
                Id = Guid.NewGuid(),
                InvoiceId = invoice.Id,
                Description = "Doctor Consultation - Standard Visit",
                Quantity = 1,
                UnitPrice = 100.00m
            });
            if (medicine != null)
            {
                invoice.LineItems.Add(new InvoiceItem
                {
                    Id = Guid.NewGuid(),
                    InvoiceId = invoice.Id,
                    Description = $"{medicine.Name} (14-day supply)",
                    Quantity = 2,
                    UnitPrice = 10.00m
                });
            }

            invoice.Payments.Add(new Payment
            {
                Id = Guid.NewGuid(),
                InvoiceId = invoice.Id,
                AmountPaid = 120.00m,
                PaymentDate = DateTime.UtcNow.AddDays(-14),
                PaymentMethod = "Credit Card (Online Portal)",
                TransactionReference = "TXN-PORTAL-883921"
            });

            var invoicePending = new Invoice
            {
                Id = Guid.NewGuid(),
                InvoiceNumber = "INV-ALICE-1002",
                PatientId = existingAlice.Id,
                IssueDate = DateTime.UtcNow.AddDays(-1),
                DueDate = DateTime.UtcNow.AddDays(29),
                SubTotal = 45.00m,
                TaxAmount = 0.00m,
                DiscountAmount = 0.00m,
                TotalAmount = 45.00m,
                Status = InvoiceStatus.Unpaid
            };

            invoicePending.LineItems.Add(new InvoiceItem
            {
                Id = Guid.NewGuid(),
                InvoiceId = invoicePending.Id,
                Description = "Diagnostic Lab Accessioning & Specimen Handling Fee",
                Quantity = 1,
                UnitPrice = 45.00m
            });

            context.Appointments.AddRange(apptPast, apptUpcoming, apptPending);
            context.VitalSigns.Add(vitals);
            context.Consultations.Add(consult);
            context.Prescriptions.Add(prescription);
            context.Invoices.AddRange(invoice, invoicePending);
            await context.SaveChangesAsync();
            Console.WriteLine("✓ Seeded Alice Patient clinical history, appointments, vitals, prescriptions, and invoices!");
        }
    }

    private static async Task SeedMedicinesAsync(ClinicDbContext context)
    {
        var medicines = new List<Medicine>
        {
            new Medicine
            {
                Code = "MED-AMOX-01",
                Name = "Amoxicillin / Clavulanate",
                GenericName = "Amoxicillin + Clavulanic Acid",
                Category = "Antibiotics",
                DosageForm = "Tablet",
                Strength = "875/125 mg",
                Manufacturer = "Sandoz Pharmaceuticals",
                StockQuantity = 150,
                ReorderLevel = 25,
                CostPrice = 8.50m,
                UnitPrice = 18.00m,
                ExpiryDate = DateTime.UtcNow.AddMonths(18),
                BatchNumber = "BATCH-AMC-2026A",
                StorageConditions = "Room Temperature (< 25°C)",
                IsControlledSubstance = false,
                IsActive = true
            },
            new Medicine
            {
                Code = "MED-AZITH-02",
                Name = "Azithromycin",
                GenericName = "Azithromycin Monohydrate",
                Category = "Antibiotics",
                DosageForm = "Tablet",
                Strength = "500 mg",
                Manufacturer = "Teva Pharmaceuticals",
                StockQuantity = 90,
                ReorderLevel = 20,
                CostPrice = 7.00m,
                UnitPrice = 15.50m,
                ExpiryDate = DateTime.UtcNow.AddMonths(14),
                BatchNumber = "BATCH-AZI-2026B",
                StorageConditions = "Room Temperature (< 25°C)",
                IsControlledSubstance = false,
                IsActive = true
            },
            new Medicine
            {
                Code = "MED-CIPRO-03",
                Name = "Ciprofloxacin",
                GenericName = "Ciprofloxacin Hydrochloride",
                Category = "Antibiotics",
                DosageForm = "Tablet",
                Strength = "500 mg",
                Manufacturer = "Bayer Healthcare",
                StockQuantity = 80,
                ReorderLevel = 15,
                CostPrice = 6.00m,
                UnitPrice = 14.00m,
                ExpiryDate = DateTime.UtcNow.AddMonths(16),
                BatchNumber = "BATCH-CIP-2026C",
                StorageConditions = "Room Temperature (< 25°C)",
                IsControlledSubstance = false,
                IsActive = true
            },
            new Medicine
            {
                Code = "MED-PARA-04",
                Name = "Paracetamol Extra",
                GenericName = "Acetaminophen / Paracetamol",
                Category = "Analgesics & Antipyretics",
                DosageForm = "Tablet",
                Strength = "500 mg",
                Manufacturer = "GSK Consumer Healthcare",
                StockQuantity = 350,
                ReorderLevel = 50,
                CostPrice = 1.20m,
                UnitPrice = 4.50m,
                ExpiryDate = DateTime.UtcNow.AddMonths(24),
                BatchNumber = "BATCH-PAR-2026D",
                StorageConditions = "Store in a dry place (< 30°C)",
                IsControlledSubstance = false,
                IsActive = true
            },
            new Medicine
            {
                Code = "MED-IBUP-05",
                Name = "Ibuprofen",
                GenericName = "Ibuprofen",
                Category = "Analgesics & Antipyretics",
                DosageForm = "Capsule",
                Strength = "400 mg",
                Manufacturer = "Abbott Laboratories",
                StockQuantity = 220,
                ReorderLevel = 30,
                CostPrice = 2.00m,
                UnitPrice = 6.00m,
                ExpiryDate = DateTime.UtcNow.AddMonths(20),
                BatchNumber = "BATCH-IBU-2026E",
                StorageConditions = "Store in a dry place (< 25°C)",
                IsControlledSubstance = false,
                IsActive = true
            },
            new Medicine
            {
                Code = "MED-AMLO-06",
                Name = "Amlodipine Besylate",
                GenericName = "Amlodipine",
                Category = "Cardiovascular",
                DosageForm = "Tablet",
                Strength = "5 mg",
                Manufacturer = "Pfizer Inc.",
                StockQuantity = 180,
                ReorderLevel = 25,
                CostPrice = 4.00m,
                UnitPrice = 11.00m,
                ExpiryDate = DateTime.UtcNow.AddMonths(22),
                BatchNumber = "BATCH-AML-2026F",
                StorageConditions = "Room Temperature (< 25°C)",
                IsControlledSubstance = false,
                IsActive = true
            },
            new Medicine
            {
                Code = "MED-LISIN-07",
                Name = "Lisinopril",
                GenericName = "Lisinopril Dihydrate",
                Category = "Cardiovascular",
                DosageForm = "Tablet",
                Strength = "10 mg",
                Manufacturer = "AstraZeneca",
                StockQuantity = 140,
                ReorderLevel = 20,
                CostPrice = 4.50m,
                UnitPrice = 12.00m,
                ExpiryDate = DateTime.UtcNow.AddMonths(19),
                BatchNumber = "BATCH-LIS-2026G",
                StorageConditions = "Room Temperature (< 25°C)",
                IsControlledSubstance = false,
                IsActive = true
            },
            new Medicine
            {
                Code = "MED-ATORV-08",
                Name = "Atorvastatin Calcium",
                GenericName = "Atorvastatin",
                Category = "Cardiovascular",
                DosageForm = "Tablet",
                Strength = "20 mg",
                Manufacturer = "Viatris",
                StockQuantity = 160,
                ReorderLevel = 25,
                CostPrice = 5.50m,
                UnitPrice = 16.00m,
                ExpiryDate = DateTime.UtcNow.AddMonths(21),
                BatchNumber = "BATCH-ATO-2026H",
                StorageConditions = "Room Temperature (< 25°C)",
                IsControlledSubstance = false,
                IsActive = true
            },
            new Medicine
            {
                Code = "MED-OMEP-09",
                Name = "Omeprazole DR",
                GenericName = "Omeprazole Delayed-Release",
                Category = "Gastrointestinal",
                DosageForm = "Capsule",
                Strength = "20 mg",
                Manufacturer = "Prilosec Pharma",
                StockQuantity = 200,
                ReorderLevel = 30,
                CostPrice = 3.50m,
                UnitPrice = 9.50m,
                ExpiryDate = DateTime.UtcNow.AddMonths(15),
                BatchNumber = "BATCH-OME-2026J",
                StorageConditions = "Store in a dry place (< 25°C)",
                IsControlledSubstance = false,
                IsActive = true
            },
            new Medicine
            {
                Code = "MED-ONDAN-10",
                Name = "Ondansetron ODT",
                GenericName = "Ondansetron",
                Category = "Gastrointestinal",
                DosageForm = "Orally Disintegrating Tablet",
                Strength = "4 mg",
                Manufacturer = "Novartis",
                StockQuantity = 110,
                ReorderLevel = 15,
                CostPrice = 4.20m,
                UnitPrice = 12.50m,
                ExpiryDate = DateTime.UtcNow.AddMonths(17),
                BatchNumber = "BATCH-OND-2026K",
                StorageConditions = "Room Temperature (< 25°C)",
                IsControlledSubstance = false,
                IsActive = true
            },
            new Medicine
            {
                Code = "MED-SALB-11",
                Name = "Salbutamol Inhaler (Ventolin)",
                GenericName = "Albuterol / Salbutamol Sulfate",
                Category = "Respiratory",
                DosageForm = "Inhaler (MDI)",
                Strength = "100 mcg / actuation (200 doses)",
                Manufacturer = "GSK Respiratory",
                StockQuantity = 45,
                ReorderLevel = 15,
                CostPrice = 12.00m,
                UnitPrice = 28.00m,
                ExpiryDate = DateTime.UtcNow.AddMonths(12),
                BatchNumber = "BATCH-SAL-2026L",
                StorageConditions = "Store below 30°C away from direct sunlight",
                IsControlledSubstance = false,
                IsActive = true
            },
            new Medicine
            {
                Code = "MED-CETI-12",
                Name = "Cetirizine HCl",
                GenericName = "Cetirizine Hydrochloride",
                Category = "Respiratory",
                DosageForm = "Tablet",
                Strength = "10 mg",
                Manufacturer = "Johnson & Johnson",
                StockQuantity = 260,
                ReorderLevel = 30,
                CostPrice = 1.80m,
                UnitPrice = 5.50m,
                ExpiryDate = DateTime.UtcNow.AddMonths(26),
                BatchNumber = "BATCH-CET-2026M",
                StorageConditions = "Room Temperature (< 25°C)",
                IsControlledSubstance = false,
                IsActive = true
            },
            new Medicine
            {
                Code = "MED-METF-13",
                Name = "Metformin HCl Extended-Release",
                GenericName = "Metformin Hydrochloride ER",
                Category = "Endocrine & Diabetes",
                DosageForm = "Tablet",
                Strength = "500 mg",
                Manufacturer = "Merck Healthcare",
                StockQuantity = 310,
                ReorderLevel = 40,
                CostPrice = 3.00m,
                UnitPrice = 8.00m,
                ExpiryDate = DateTime.UtcNow.AddMonths(20),
                BatchNumber = "BATCH-MET-2026N",
                StorageConditions = "Room Temperature (< 25°C)",
                IsControlledSubstance = false,
                IsActive = true
            },
            new Medicine
            {
                Code = "MED-GLIM-14",
                Name = "Glimepiride",
                GenericName = "Glimepiride",
                Category = "Endocrine & Diabetes",
                DosageForm = "Tablet",
                Strength = "2 mg",
                Manufacturer = "Sanofi",
                StockQuantity = 175,
                ReorderLevel = 25,
                CostPrice = 2.80m,
                UnitPrice = 7.50m,
                ExpiryDate = DateTime.UtcNow.AddMonths(18),
                BatchNumber = "BATCH-GLI-2026P",
                StorageConditions = "Room Temperature (< 25°C)",
                IsControlledSubstance = false,
                IsActive = true
            },
            new Medicine
            {
                Code = "MED-MORPH-15",
                Name = "Morphine Sulfate Oral Solution",
                GenericName = "Morphine Sulfate",
                Category = "Controlled Substances",
                DosageForm = "Oral Solution",
                Strength = "10 mg / 5 mL (100 mL bottle)",
                Manufacturer = "Purdue Pharma",
                StockQuantity = 12,
                ReorderLevel = 10,
                CostPrice = 24.00m,
                UnitPrice = 55.00m,
                ExpiryDate = DateTime.UtcNow.AddMonths(9),
                BatchNumber = "BATCH-NAR-9001",
                StorageConditions = "Double-locked Controlled Drug Safe (15-25°C)",
                IsControlledSubstance = true,
                IsActive = true
            },
            new Medicine
            {
                Code = "MED-TRAM-16",
                Name = "Tramadol HCl",
                GenericName = "Tramadol Hydrochloride",
                Category = "Controlled Substances",
                DosageForm = "Capsule",
                Strength = "50 mg",
                Manufacturer = "Grunenthal",
                StockQuantity = 18,
                ReorderLevel = 15,
                CostPrice = 8.00m,
                UnitPrice = 22.00m,
                ExpiryDate = DateTime.UtcNow.AddMonths(11),
                BatchNumber = "BATCH-NAR-9002",
                StorageConditions = "Double-locked Controlled Drug Safe (15-25°C)",
                IsControlledSubstance = true,
                IsActive = true
            },
            new Medicine
            {
                Code = "MED-DIAZ-17",
                Name = "Diazepam",
                GenericName = "Diazepam (Valium)",
                Category = "Controlled Substances",
                DosageForm = "Tablet",
                Strength = "5 mg",
                Manufacturer = "Roche Pharmaceuticals",
                StockQuantity = 14,
                ReorderLevel = 12,
                CostPrice = 6.00m,
                UnitPrice = 19.50m,
                ExpiryDate = DateTime.UtcNow.AddMonths(15),
                BatchNumber = "BATCH-NAR-9003",
                StorageConditions = "Double-locked Controlled Drug Safe (15-25°C)",
                IsControlledSubstance = true,
                IsActive = true
            },
            new Medicine
            {
                Code = "MED-HYDRO-18",
                Name = "Hydrocortisone Cream 1%",
                GenericName = "Hydrocortisone",
                Category = "Dermatological",
                DosageForm = "Topical Cream",
                Strength = "1% w/w (30g tube)",
                Manufacturer = "Perrigo Company",
                StockQuantity = 65,
                ReorderLevel = 15,
                CostPrice = 3.50m,
                UnitPrice = 9.00m,
                ExpiryDate = DateTime.UtcNow.AddMonths(18),
                BatchNumber = "BATCH-DER-2026Q",
                StorageConditions = "Store below 25°C. Do not freeze",
                IsControlledSubstance = false,
                IsActive = true
            },
            new Medicine
            {
                Code = "MED-CIPRO-EYE",
                Name = "Ciprofloxacin Ophthalmic Solution",
                GenericName = "Ciprofloxacin 0.3%",
                Category = "Ophthalmic & Otic",
                DosageForm = "Eye / Ear Drops",
                Strength = "0.3% (5 mL)",
                Manufacturer = "Alcon Laboratories",
                StockQuantity = 40,
                ReorderLevel = 10,
                CostPrice = 5.00m,
                UnitPrice = 14.50m,
                ExpiryDate = DateTime.UtcNow.AddMonths(10),
                BatchNumber = "BATCH-OPH-2026R",
                StorageConditions = "Store at 2-25°C",
                IsControlledSubstance = false,
                IsActive = true
            },
            new Medicine
            {
                Code = "MED-VITD-20",
                Name = "Vitamin D3 High Potency",
                GenericName = "Cholecalciferol",
                Category = "Vitamins & Supplements",
                DosageForm = "Capsule",
                Strength = "50,000 IU",
                Manufacturer = "Bio-Tech Pharmacal",
                StockQuantity = 120,
                ReorderLevel = 20,
                CostPrice = 4.00m,
                UnitPrice = 12.00m,
                ExpiryDate = DateTime.UtcNow.AddMonths(24),
                BatchNumber = "BATCH-VIT-2026S",
                StorageConditions = "Protect from light",
                IsControlledSubstance = false,
                IsActive = true
            },
            new Medicine
            {
                Code = "MED-EPI-21",
                Name = "Epinephrine Auto-Injector",
                GenericName = "Epinephrine",
                Category = "Emergency & Resuscitation",
                DosageForm = "Auto-Injector",
                Strength = "0.3 mg / 0.3 mL",
                Manufacturer = "Mylan Specialty",
                StockQuantity = 7, // Intentionally < 10 to demonstrate low-stock alert!
                ReorderLevel = 10,
                CostPrice = 65.00m,
                UnitPrice = 145.00m,
                ExpiryDate = DateTime.UtcNow.AddMonths(8),
                BatchNumber = "BATCH-EMR-2026T",
                StorageConditions = "Store at 20-25°C, protect from light",
                IsControlledSubstance = false,
                IsActive = true
            }
        };

        foreach (var med in medicines)
        {
            var existing = await context.Medicines.FirstOrDefaultAsync(m => m.Code == med.Code);
            if (existing == null)
            {
                context.Medicines.Add(med);
            }
            else
            {
                // Update properties with formulary details
                existing.GenericName = med.GenericName;
                existing.DosageForm = med.DosageForm;
                existing.Strength = med.Strength;
                existing.Manufacturer = med.Manufacturer;
                existing.Category = med.Category;
                existing.ReorderLevel = med.ReorderLevel;
                existing.CostPrice = med.CostPrice;
                existing.StorageConditions = med.StorageConditions;
                existing.IsControlledSubstance = med.IsControlledSubstance;
                existing.IsActive = med.IsActive;
            }
        }
        await context.SaveChangesAsync();
    }

    private static async Task SeedFinanceDataAsync(ClinicDbContext context)
    {
        // 1. Tag existing invoices with departments if unassigned
        var existingInvoices = await context.Invoices.Include(i => i.LineItems).ToListAsync();
        foreach (var inv in existingInvoices)
        {
            if (string.IsNullOrWhiteSpace(inv.Department))
            {
                if (inv.LineItems.Any(li => li.Description.Contains("Prescription", StringComparison.OrdinalIgnoreCase) || li.Description.Contains("supply", StringComparison.OrdinalIgnoreCase) || li.Description.Contains("Amoxicillin", StringComparison.OrdinalIgnoreCase)))
                {
                    inv.Department = "Pharmacy";
                }
                else if (inv.LineItems.Any(li => li.Description.Contains("Lab", StringComparison.OrdinalIgnoreCase) || li.Description.Contains("Blood", StringComparison.OrdinalIgnoreCase)))
                {
                    inv.Department = "Laboratory";
                }
                else if (inv.LineItems.Any(li => li.Description.Contains("Radiology", StringComparison.OrdinalIgnoreCase) || li.Description.Contains("X-Ray", StringComparison.OrdinalIgnoreCase) || li.Description.Contains("MRI", StringComparison.OrdinalIgnoreCase) || li.Description.Contains("CT", StringComparison.OrdinalIgnoreCase) || li.Description.Contains("Ultrasound", StringComparison.OrdinalIgnoreCase) || li.Description.Contains("Sonogram", StringComparison.OrdinalIgnoreCase)))
                {
                    inv.Department = "Radiology";
                }
                else
                {
                    inv.Department = "Consultation";
                }
            }
        }
        await context.SaveChangesAsync();

        var alicePatient = await context.Patients.FirstOrDefaultAsync();
        var patientId = alicePatient?.Id ?? Guid.NewGuid();
        var accountant = await context.Users.FirstOrDefaultAsync(u => u.Username == "accountant.dave");
        var accountantId = accountant?.Id ?? Guid.NewGuid();
        var firstInvoice = existingInvoices.FirstOrDefault();

        // 2. Insurance Claims
        if (!await context.InsuranceClaims.AnyAsync() && firstInvoice != null)
        {
            var claims = new List<InsuranceClaim>
            {
                new InsuranceClaim
                {
                    Id = Guid.NewGuid(),
                    ClaimNumber = "CLM-20260901-1001",
                    InvoiceId = firstInvoice.Id,
                    PatientId = patientId,
                    InsuranceProvider = "BlueCross BlueShield",
                    PolicyNumber = "BCBS-994821",
                    GroupNumber = "GRP-TECH-44",
                    ClaimAmount = 120.00m,
                    CoveredAmount = 96.00m,
                    PatientCopay = 24.00m,
                    Status = "Remitted",
                    RemittanceReference = "EFT-BCBS-89312",
                    SubmittedAt = DateTime.UtcNow.AddDays(-10),
                    SettledAt = DateTime.UtcNow.AddDays(-2),
                    Notes = "In-network primary consultation & prescription copay settled"
                },
                new InsuranceClaim
                {
                    Id = Guid.NewGuid(),
                    ClaimNumber = "CLM-20260912-1002",
                    InvoiceId = firstInvoice.Id,
                    PatientId = patientId,
                    InsuranceProvider = "Aetna Health",
                    PolicyNumber = "AET-77192",
                    GroupNumber = "GRP-STATE-12",
                    ClaimAmount = 240.00m,
                    CoveredAmount = 0m,
                    PatientCopay = 40.00m,
                    Status = "InReview",
                    SubmittedAt = DateTime.UtcNow.AddDays(-4),
                    Notes = "Diagnostic comprehensive blood panel undergoing medical necessity adjudication"
                },
                new InsuranceClaim
                {
                    Id = Guid.NewGuid(),
                    ClaimNumber = "CLM-20260914-1003",
                    InvoiceId = firstInvoice.Id,
                    PatientId = patientId,
                    InsuranceProvider = "UnitedHealthcare",
                    PolicyNumber = "UHC-55102",
                    GroupNumber = "GRP-EDU-9",
                    ClaimAmount = 185.00m,
                    CoveredAmount = 0m,
                    PatientCopay = 185.00m,
                    Status = "Denied",
                    DenialReason = "CO-16: Lacks information needed for adjudication (missing specialist referral doc)",
                    SubmittedAt = DateTime.UtcNow.AddDays(-6),
                    SettledAt = DateTime.UtcNow.AddDays(-1),
                    Notes = "Appeal queued with attached referral form from Dr. Smith"
                },
                new InsuranceClaim
                {
                    Id = Guid.NewGuid(),
                    ClaimNumber = "CLM-20260915-1004",
                    InvoiceId = firstInvoice.Id,
                    PatientId = patientId,
                    InsuranceProvider = "Cigna Global",
                    PolicyNumber = "CIG-33819",
                    GroupNumber = "GRP-CORP-88",
                    ClaimAmount = 310.00m,
                    CoveredAmount = 275.00m,
                    PatientCopay = 35.00m,
                    Status = "Approved",
                    RemittanceReference = "EFT-CIG-22910",
                    SubmittedAt = DateTime.UtcNow.AddDays(-3),
                    SettledAt = DateTime.UtcNow.AddDays(-1),
                    Notes = "Pre-authorized routine preventive care claim approved"
                }
            };
            context.InsuranceClaims.AddRange(claims);
        }

        // 3. Billing Adjustments
        if (!await context.BillingAdjustments.AnyAsync() && firstInvoice != null)
        {
            var adjustments = new List<BillingAdjustment>
            {
                new BillingAdjustment
                {
                    Id = Guid.NewGuid(),
                    AdjustmentNumber = "ADJ-20260905-01",
                    InvoiceId = firstInvoice.Id,
                    Type = "Discount",
                    Amount = 18.00m,
                    Reason = "Senior Citizen courtesy 15% discount applied per clinic policy",
                    ApprovedByUserId = accountantId,
                    ApprovedAt = DateTime.UtcNow.AddDays(-7),
                    Status = "Approved",
                    RequestedByUserId = accountantId,
                    CreatedAt = DateTime.UtcNow.AddDays(-7)
                },
                new BillingAdjustment
                {
                    Id = Guid.NewGuid(),
                    AdjustmentNumber = "ADJ-20260910-02",
                    InvoiceId = firstInvoice.Id,
                    Type = "BadDebtWriteOff",
                    Amount = 45.00m,
                    Reason = "Indigent patient compassionate care write-off approved by Medical Director",
                    ApprovedByUserId = accountantId,
                    ApprovedAt = DateTime.UtcNow.AddDays(-3),
                    Status = "Approved",
                    RequestedByUserId = accountantId,
                    CreatedAt = DateTime.UtcNow.AddDays(-3)
                },
                new BillingAdjustment
                {
                    Id = Guid.NewGuid(),
                    AdjustmentNumber = "ADJ-20260918-03",
                    InvoiceId = firstInvoice.Id,
                    Type = "Refund",
                    Amount = 25.00m,
                    Reason = "Patient overpaid on credit card terminal due to network retry duplicate charge",
                    Status = "PendingApproval",
                    RequestedByUserId = accountantId,
                    CreatedAt = DateTime.UtcNow.AddHours(-5)
                }
            };
            context.BillingAdjustments.AddRange(adjustments);
        }

        // 4. Expenses & Accounts Payable (AP)
        if (!await context.ExpenseTransactions.AnyAsync())
        {
            var pharmacyPO = await context.PharmacyStockRequisitions.FirstOrDefaultAsync(r => r.Type == "PurchaseOrder");

            var expenses = new List<ExpenseTransaction>
            {
                new ExpenseTransaction
                {
                    Id = Guid.NewGuid(),
                    ExpenseNumber = "EXP-20260902-101",
                    Category = "PharmaceuticalSupplier",
                    VendorName = "McKesson Medical-Surgical",
                    InvoiceReference = "INV-MCK-881920",
                    Description = "Bulk antibiotic & cardiovascular medicine replenishment batch",
                    Amount = 1450.00m,
                    TaxAmount = 72.50m,
                    DueDate = DateTime.UtcNow.AddDays(15),
                    PaidDate = DateTime.UtcNow.AddDays(-2),
                    PaymentMethod = "ACH Direct Debit",
                    Status = "Paid",
                    PharmacyStockRequisitionId = pharmacyPO?.Id,
                    RecordedByUserId = accountantId,
                    ApprovedByUserId = accountantId,
                    CreatedAt = DateTime.UtcNow.AddDays(-12),
                    Notes = "Three-way match verified against warehouse receiving slip"
                },
                new ExpenseTransaction
                {
                    Id = Guid.NewGuid(),
                    ExpenseNumber = "EXP-20260910-102",
                    Category = "MedicalSupplies",
                    VendorName = "Medline Industries",
                    InvoiceReference = "INV-MED-44910",
                    Description = "Sterile gloves, syringes, IV cannulas & protective PPE cartons",
                    Amount = 520.00m,
                    TaxAmount = 26.00m,
                    DueDate = DateTime.UtcNow.AddDays(10),
                    PaymentMethod = "BankTransfer",
                    Status = "PendingApproval",
                    RecordedByUserId = accountantId,
                    CreatedAt = DateTime.UtcNow.AddDays(-3),
                    Notes = "Awaiting departmental supervisor signoff"
                },
                new ExpenseTransaction
                {
                    Id = Guid.NewGuid(),
                    ExpenseNumber = "EXP-20260912-103",
                    Category = "WasteDisposal",
                    VendorName = "Stericycle Biohazard Management",
                    InvoiceReference = "INV-BIO-3321",
                    Description = "Sharps bins and biological medical waste destruction service",
                    Amount = 340.00m,
                    TaxAmount = 0.00m,
                    DueDate = DateTime.UtcNow.AddDays(20),
                    PaidDate = DateTime.UtcNow.AddDays(-4),
                    PaymentMethod = "CreditCard",
                    Status = "Paid",
                    RecordedByUserId = accountantId,
                    ApprovedByUserId = accountantId,
                    CreatedAt = DateTime.UtcNow.AddDays(-8)
                },
                new ExpenseTransaction
                {
                    Id = Guid.NewGuid(),
                    ExpenseNumber = "EXP-20260916-104",
                    Category = "EquipmentMaintenance",
                    VendorName = "GE Healthcare Life Care",
                    InvoiceReference = "INV-GE-9081",
                    Description = "Ultrasound scanner & ECG telemetry bi-annual calibration",
                    Amount = 850.00m,
                    TaxAmount = 42.50m,
                    DueDate = DateTime.UtcNow.AddDays(12),
                    PaymentMethod = "BankTransfer",
                    Status = "Approved",
                    RecordedByUserId = accountantId,
                    ApprovedByUserId = accountantId,
                    CreatedAt = DateTime.UtcNow.AddDays(-2),
                    Notes = "Certified compliance report on file"
                }
            };
            context.ExpenseTransactions.AddRange(expenses);
        }

        // 5. Petty Cash Vouchers
        if (!await context.PettyCashVouchers.AnyAsync())
        {
            var vouchers = new List<PettyCashVoucher>
            {
                new PettyCashVoucher
                {
                    Id = Guid.NewGuid(),
                    VoucherNumber = "PCV-20260901-01",
                    Type = "Replenishment",
                    Amount = 500.00m,
                    Category = "PettyCashReplenish",
                    Payee = "First National Bank Cashier",
                    Description = "Monthly petty cash float replenishment authorized by Dave",
                    DisbursedByUserId = accountantId,
                    DisbursedByUserName = "Dave Accountant",
                    RemainingBalance = 500.00m,
                    CreatedAt = DateTime.UtcNow.AddDays(-15),
                    ReceiptRef = "FLOAT-202609-01"
                },
                new PettyCashVoucher
                {
                    Id = Guid.NewGuid(),
                    VoucherNumber = "PCV-20260905-02",
                    Type = "Disbursement",
                    Amount = 35.00m,
                    Category = "UrgentCourier",
                    Payee = "Metro Courier Express",
                    Description = "Urgent cold-chain transport of biopsy specimen to Central Regional Lab",
                    DisbursedByUserId = accountantId,
                    DisbursedByUserName = "Dave Accountant",
                    RemainingBalance = 465.00m,
                    CreatedAt = DateTime.UtcNow.AddDays(-11),
                    ReceiptRef = "REC-MTR-991"
                },
                new PettyCashVoucher
                {
                    Id = Guid.NewGuid(),
                    VoucherNumber = "PCV-20260910-03",
                    Type = "Disbursement",
                    Amount = 42.50m,
                    Category = "Breakroom",
                    Payee = "Costco Wholesale",
                    Description = "Waiting room patient refreshment supplies (spring water & tea sets)",
                    DisbursedByUserId = accountantId,
                    DisbursedByUserName = "Dave Accountant",
                    RemainingBalance = 422.50m,
                    CreatedAt = DateTime.UtcNow.AddDays(-6),
                    ReceiptRef = "REC-CST-4412"
                },
                new PettyCashVoucher
                {
                    Id = Guid.NewGuid(),
                    VoucherNumber = "PCV-20260915-04",
                    Type = "Disbursement",
                    Amount = 20.00m,
                    Category = "LocalTravel",
                    Payee = "City Cab Express",
                    Description = "Emergency taxi transport voucher for elderly mobility-impaired patient",
                    DisbursedByUserId = accountantId,
                    DisbursedByUserName = "Dave Accountant",
                    RemainingBalance = 402.50m,
                    CreatedAt = DateTime.UtcNow.AddDays(-2),
                    ReceiptRef = "REC-TX-102"
                }
            };
            context.PettyCashVouchers.AddRange(vouchers);
        }

        // 6. Staff Payroll Records
        if (!await context.PayrollRecords.AnyAsync())
        {
            var doctorUser = await context.Users.FirstOrDefaultAsync(u => u.Username == "dr.smith");
            var nurseUser = await context.Users.FirstOrDefaultAsync(u => u.Username == "nurse.joy");
            var recepUser = await context.Users.FirstOrDefaultAsync(u => u.Username == "receptionist.clara");

            var payrolls = new List<PayrollRecord>
            {
                new PayrollRecord
                {
                    Id = Guid.NewGuid(),
                    PayrollNumber = "PAY-202609-DR-SMITH",
                    StaffUserId = doctorUser?.Id ?? Guid.NewGuid(),
                    StaffName = "Dr. John Smith",
                    Role = "Doctor",
                    PeriodMonth = 9,
                    PeriodYear = 2026,
                    BaseSalary = 7500.00m,
                    ConsultationsCount = 46,
                    DoctorCommissionShare = 920.00m,
                    OvertimeHours = 0,
                    OvertimePay = 0,
                    TaxDeductions = 1263.00m,
                    NetSalaryPayable = 7157.00m,
                    Status = "Disbursed",
                    DisbursedAt = DateTime.UtcNow.AddDays(-1),
                    PaymentReference = "ACH-PR-202609-SMITH",
                    CreatedAt = DateTime.UtcNow.AddDays(-3)
                },
                new PayrollRecord
                {
                    Id = Guid.NewGuid(),
                    PayrollNumber = "PAY-202609-NURSE-JOY",
                    StaffUserId = nurseUser?.Id ?? Guid.NewGuid(),
                    StaffName = "Joy Nurse",
                    Role = "Nurse",
                    PeriodMonth = 9,
                    PeriodYear = 2026,
                    BaseSalary = 4200.00m,
                    ConsultationsCount = 0,
                    DoctorCommissionShare = 0,
                    OvertimeHours = 8.0m,
                    OvertimePay = 280.00m,
                    TaxDeductions = 672.00m,
                    NetSalaryPayable = 3808.00m,
                    Status = "Disbursed",
                    DisbursedAt = DateTime.UtcNow.AddDays(-1),
                    PaymentReference = "ACH-PR-202609-JOY",
                    CreatedAt = DateTime.UtcNow.AddDays(-3)
                },
                new PayrollRecord
                {
                    Id = Guid.NewGuid(),
                    PayrollNumber = "PAY-202609-ACC-DAVE",
                    StaffUserId = accountantId,
                    StaffName = "Dave Accountant",
                    Role = "Accountant",
                    PeriodMonth = 9,
                    PeriodYear = 2026,
                    BaseSalary = 4800.00m,
                    ConsultationsCount = 0,
                    DoctorCommissionShare = 0,
                    OvertimeHours = 0,
                    OvertimePay = 0,
                    TaxDeductions = 720.00m,
                    NetSalaryPayable = 4080.00m,
                    Status = "Disbursed",
                    DisbursedAt = DateTime.UtcNow.AddDays(-1),
                    PaymentReference = "ACH-PR-202609-DAVE",
                    CreatedAt = DateTime.UtcNow.AddDays(-3)
                },
                new PayrollRecord
                {
                    Id = Guid.NewGuid(),
                    PayrollNumber = "PAY-202609-REC-CLARA",
                    StaffUserId = recepUser?.Id ?? Guid.NewGuid(),
                    StaffName = "Clara Receptionist",
                    Role = "Receptionist",
                    PeriodMonth = 9,
                    PeriodYear = 2026,
                    BaseSalary = 3600.00m,
                    ConsultationsCount = 0,
                    DoctorCommissionShare = 0,
                    OvertimeHours = 6.0m,
                    OvertimePay = 210.00m,
                    TaxDeductions = 571.50m,
                    NetSalaryPayable = 3238.50m,
                    Status = "Approved",
                    CreatedAt = DateTime.UtcNow.AddDays(-3)
                }
            };
            context.PayrollRecords.AddRange(payrolls);
        }

        // 7. Update DailyCashReconciliations with audit verification
        var reconciliations = await context.DailyCashReconciliations.ToListAsync();
        if (reconciliations.Any())
        {
            var firstReconciliation = reconciliations.First();
            if (string.IsNullOrWhiteSpace(firstReconciliation.AuditStatus))
            {
                firstReconciliation.AuditStatus = "VerifiedBalanced";
                firstReconciliation.AuditedByUserId = accountantId;
                firstReconciliation.AuditedByUserName = "Dave Accountant";
                firstReconciliation.AuditedAt = DateTime.UtcNow.AddHours(-12);
                firstReconciliation.AuditNotes = "Drawer cash counted & reconciled with merchant terminal settlement batch. Zero discrepancy.";
            }

            // Ensure there is at least one pending reconciliation for accountant to audit
            if (!reconciliations.Any(r => r.AuditStatus == "Pending" || string.IsNullOrWhiteSpace(r.AuditStatus)))
            {
                var recepUser = await context.Users.FirstOrDefaultAsync(u => u.Username == "receptionist.clara");
                var pendingDrawer = new DailyCashReconciliation
                {
                    Id = Guid.NewGuid(),
                    ReceptionistUserId = recepUser?.Id ?? Guid.NewGuid(),
                    ReconciliationDate = DateTime.UtcNow.Date,
                    Shift = "Morning Shift (08:00 - 16:00)",
                    CashCollected = 450.00m,
                    CardCollected = 780.00m,
                    DigitalCollected = 120.00m,
                    TotalCollected = 1350.00m,
                    ClosingCashCounted = 450.00m,
                    DiscrepancyAmount = 0.00m,
                    InvoicesSettledCount = 8,
                    Status = "Balanced",
                    AuditStatus = "Pending",
                    Notes = "End of morning shift cash handover to vault",
                    CreatedAt = DateTime.UtcNow.AddHours(-2)
                };
                context.DailyCashReconciliations.Add(pendingDrawer);
            }
        }

        await context.SaveChangesAsync();
        Console.WriteLine("✓ Seeded Accountant & Finance suite: claims, adjustments, AP expenses, petty cash, payroll, and drawer audits!");
    }

    private static async Task SeedLaboratorySuiteAsync(ClinicDbContext context)
    {
        var labTechUser = await context.Users.FirstOrDefaultAsync(u => u.Username == "lab.tech");
        var labTechId = labTechUser?.Id ?? Guid.NewGuid();
        var labTechName = labTechUser != null ? $"{labTechUser.FirstName} {labTechUser.LastName}" : "Leo Technician";

        var doctor = await context.Users.FirstOrDefaultAsync(u => u.Username == "dr.smith") 
            ?? await context.Users.FirstOrDefaultAsync(u => u.UserRoles.Any(ur => ur.Role.Name == "Doctor"));
        var doctorId = doctor?.Id ?? Guid.NewGuid();

        var patient = await context.Patients.FirstOrDefaultAsync(p => p.MedicalRecordNumber == "PAT-ALICE-001")
            ?? await context.Patients.FirstOrDefaultAsync();

        if (patient == null) return;

        // 1. Seed Reagents if empty
        if (!await context.LabReagents.AnyAsync())
        {
            var reagents = new List<LabReagent>
            {
                new LabReagent
                {
                    Name = "Sysmex CBC Lyse Reagent",
                    CatalogCode = "REA-CBC-001",
                    Category = "Hematology",
                    LotNumber = "LYS-9921",
                    CurrentStock = 24,
                    MinimumThreshold = 10,
                    UnitOfMeasure = "Bottles",
                    ExpiryDate = DateTime.UtcNow.AddMonths(8),
                    StorageCondition = "15-25°C Room Temp",
                    Status = "InStock",
                    Manufacturer = "Sysmex Corporation",
                    UnitCost = 45.00m,
                    LastRestockedAt = DateTime.UtcNow.AddDays(-10),
                    Notes = "For differential leukocyte analysis on XN-550"
                },
                new LabReagent
                {
                    Name = "Sysmex Cellpack DCL Diluent",
                    CatalogCode = "REA-CBC-002",
                    Category = "Hematology",
                    LotNumber = "DCL-4412",
                    CurrentStock = 4,
                    MinimumThreshold = 8,
                    UnitOfMeasure = "Kits (20L)",
                    ExpiryDate = DateTime.UtcNow.AddMonths(11),
                    StorageCondition = "15-25°C Room Temp",
                    Status = "LowStock",
                    Manufacturer = "Sysmex Corporation",
                    UnitCost = 85.00m,
                    LastRestockedAt = DateTime.UtcNow.AddDays(-25),
                    Notes = "Diluent below reorder threshold. Requisition pending."
                },
                new LabReagent
                {
                    Name = "Roche Cobas Glucose HK Reagent",
                    CatalogCode = "REA-BIO-003",
                    Category = "Biochemistry",
                    LotNumber = "GLU-8820",
                    CurrentStock = 32,
                    MinimumThreshold = 15,
                    UnitOfMeasure = "Tests (200/pk)",
                    ExpiryDate = DateTime.UtcNow.AddMonths(5),
                    StorageCondition = "2-8°C Refrigerated",
                    Status = "InStock",
                    Manufacturer = "Roche Diagnostics",
                    UnitCost = 120.00m,
                    LastRestockedAt = DateTime.UtcNow.AddDays(-5),
                    Notes = "Hexokinase enzymatic method for plasma glucose"
                },
                new LabReagent
                {
                    Name = "Roche Cobas Lipid Panel Calibrator",
                    CatalogCode = "REA-LIP-004",
                    Category = "Lipid",
                    LotNumber = "CAL-1102",
                    CurrentStock = 2,
                    MinimumThreshold = 5,
                    UnitOfMeasure = "Vials",
                    ExpiryDate = DateTime.UtcNow.AddMonths(2),
                    StorageCondition = "2-8°C Refrigerated",
                    Status = "LowStock",
                    Manufacturer = "Roche Diagnostics",
                    UnitCost = 65.00m,
                    LastRestockedAt = DateTime.UtcNow.AddDays(-40),
                    Notes = "Calibrator for TC, HDL-C, and Triglycerides assay"
                },
                new LabReagent
                {
                    Name = "Roche Creatinine Jaffe Reagent",
                    CatalogCode = "REA-BIO-005",
                    Category = "Biochemistry",
                    LotNumber = "CRE-5541",
                    CurrentStock = 18,
                    MinimumThreshold = 10,
                    UnitOfMeasure = "Tests (150/pk)",
                    ExpiryDate = DateTime.UtcNow.AddMonths(9),
                    StorageCondition = "2-8°C Refrigerated",
                    Status = "InStock",
                    Manufacturer = "Roche Diagnostics",
                    UnitCost = 95.00m,
                    LastRestockedAt = DateTime.UtcNow.AddDays(-15),
                    Notes = "Rate-blanked compensated Jaffe method"
                },
                new LabReagent
                {
                    Name = "EDTA Lavender Top Blood Tubes (4ml)",
                    CatalogCode = "CON-TUB-006",
                    Category = "Consumables",
                    LotNumber = "TB-7741",
                    CurrentStock = 450,
                    MinimumThreshold = 100,
                    UnitOfMeasure = "Tubes",
                    ExpiryDate = DateTime.UtcNow.AddYears(1),
                    StorageCondition = "15-25°C Room Temp",
                    Status = "InStock",
                    Manufacturer = "BD Vacutainer",
                    UnitCost = 0.85m,
                    LastRestockedAt = DateTime.UtcNow.AddDays(-7),
                    Notes = "K2 EDTA anticoagulant for whole blood specimens"
                },
                new LabReagent
                {
                    Name = "SST Gold Top Gel Separator Tubes (5ml)",
                    CatalogCode = "CON-TUB-007",
                    Category = "Consumables",
                    LotNumber = "TB-8890",
                    CurrentStock = 380,
                    MinimumThreshold = 100,
                    UnitOfMeasure = "Tubes",
                    ExpiryDate = DateTime.UtcNow.AddYears(1),
                    StorageCondition = "15-25°C Room Temp",
                    Status = "InStock",
                    Manufacturer = "BD Vacutainer",
                    UnitCost = 0.95m,
                    LastRestockedAt = DateTime.UtcNow.AddDays(-7),
                    Notes = "Clot activator & polymer gel for serum determinations"
                },
                new LabReagent
                {
                    Name = "Rapid COVID-19/Flu A+B Dual Antigen Swabs",
                    CatalogCode = "KIT-INF-008",
                    Category = "Microbiology",
                    LotNumber = "AG-3321",
                    CurrentStock = 60,
                    MinimumThreshold = 25,
                    UnitOfMeasure = "Kits (25/box)",
                    ExpiryDate = DateTime.UtcNow.AddMonths(4),
                    StorageCondition = "15-25°C Room Temp",
                    Status = "InStock",
                    Manufacturer = "Abbott Diagnostics",
                    UnitCost = 18.50m,
                    LastRestockedAt = DateTime.UtcNow.AddDays(-12),
                    Notes = "Point-of-care rapid lateral flow assay"
                }
            };
            context.LabReagents.AddRange(reagents);
            await context.SaveChangesAsync();
        }

        // 2. Seed Lab Stock Requisitions if empty
        if (!await context.LabStockRequisitions.AnyAsync())
        {
            var cellpack = await context.LabReagents.FirstOrDefaultAsync(r => r.CatalogCode == "REA-CBC-002");
            var calibrator = await context.LabReagents.FirstOrDefaultAsync(r => r.CatalogCode == "REA-LIP-004");
            var tubes = await context.LabReagents.FirstOrDefaultAsync(r => r.CatalogCode == "CON-TUB-006");

            var requisitions = new List<LabStockRequisition>
            {
                new LabStockRequisition
                {
                    RequisitionNumber = "REQ-2026-0001",
                    LabReagentId = cellpack?.Id,
                    ItemName = "Sysmex Cellpack DCL Diluent",
                    Category = "Hematology",
                    QuantityRequested = 12,
                    UnitOfMeasure = "Kits (20L)",
                    EstimatedUnitCost = 85.00m,
                    TotalEstimatedCost = 1020.00m,
                    Urgency = "Urgent",
                    Reason = "Current stock is 4 units (threshold is 8). Critical for daily routine CBC runs.",
                    Status = "Pending",
                    RequestedByUserId = labTechId,
                    RequestedByUserName = labTechName,
                    CreatedAt = DateTime.UtcNow.AddHours(-18)
                },
                new LabStockRequisition
                {
                    RequisitionNumber = "REQ-2026-0002",
                    LabReagentId = calibrator?.Id,
                    ItemName = "Roche Cobas Lipid Panel Calibrator",
                    Category = "Lipid",
                    QuantityRequested = 5,
                    UnitOfMeasure = "Vials",
                    EstimatedUnitCost = 65.00m,
                    TotalEstimatedCost = 325.00m,
                    Urgency = "High",
                    Reason = "Monthly QC calibration cycle approaching next week. Current stock is 2 vials.",
                    Status = "Approved",
                    RequestedByUserId = labTechId,
                    RequestedByUserName = labTechName,
                    ApprovedByUserId = doctorId,
                    ApprovedByUserName = "Dr. John Smith",
                    ReviewedAt = DateTime.UtcNow.AddHours(-6),
                    ReviewNotes = "Approved by Laboratory Director. PO dispatched to vendor.",
                    CreatedAt = DateTime.UtcNow.AddDays(-2)
                },
                new LabStockRequisition
                {
                    RequisitionNumber = "REQ-2026-0003",
                    LabReagentId = tubes?.Id,
                    ItemName = "EDTA Lavender Top Blood Tubes (4ml)",
                    Category = "Consumables",
                    QuantityRequested = 500,
                    UnitOfMeasure = "Tubes",
                    EstimatedUnitCost = 0.85m,
                    TotalEstimatedCost = 425.00m,
                    Urgency = "Routine",
                    Reason = "Quarterly consumable replenishment for phlebotomy drawing stations.",
                    Status = "Fulfilled",
                    RequestedByUserId = labTechId,
                    RequestedByUserName = labTechName,
                    ApprovedByUserId = doctorId,
                    ApprovedByUserName = "Dr. John Smith",
                    ReviewedAt = DateTime.UtcNow.AddDays(-5),
                    ReviewNotes = "Goods received and stored in room-temp consumable cabinet.",
                    CreatedAt = DateTime.UtcNow.AddDays(-7)
                }
            };
            context.LabStockRequisitions.AddRange(requisitions);
            await context.SaveChangesAsync();
        }

        // 3. Seed Lab Equipment Logs if empty
        if (!await context.LabEquipmentLogs.AnyAsync())
        {
            var logs = new List<LabEquipmentLog>
            {
                new LabEquipmentLog
                {
                    EquipmentName = "Sysmex XN-550 Automated Hematology Analyzer",
                    EquipmentModel = "XN-550",
                    SerialNumber = "SYX-550-99824",
                    Location = "Main Hematology Bench A",
                    LogType = "DailyCalibration",
                    ControlLotNumber = "E-CHECK-901B",
                    StandardDeviationScore = 0.24m,
                    Status = "Passed",
                    Findings = "Tri-level whole blood controls run. WBC, RBC, HGB, PLT all within 1.0 SD of target mean. Flow cell laser aperture clean.",
                    PerformedByUserId = labTechId,
                    PerformedByUserName = labTechName,
                    PerformedAt = DateTime.UtcNow.AddHours(-4),
                    NextMaintenanceDueDate = DateTime.UtcNow.AddDays(7),
                    CreatedAt = DateTime.UtcNow.AddHours(-4)
                },
                new LabEquipmentLog
                {
                    EquipmentName = "Roche Cobas c311 Chemistry Analyzer",
                    EquipmentModel = "Cobas c311",
                    SerialNumber = "ROC-311-55219",
                    Location = "Clinical Chemistry Suite",
                    LogType = "DailyCalibration",
                    ControlLotNumber = "PRECICONTROL-CLIN-404",
                    StandardDeviationScore = 0.38m,
                    Status = "Passed",
                    Findings = "Electrolyte ISE slope check normal (Na+ 58.2 mV/dec, K+ 57.8 mV/dec). Cuvette ring temperature nominal at 37.0°C.",
                    PerformedByUserId = labTechId,
                    PerformedByUserName = labTechName,
                    PerformedAt = DateTime.UtcNow.AddHours(-5),
                    NextMaintenanceDueDate = DateTime.UtcNow.AddDays(1),
                    CreatedAt = DateTime.UtcNow.AddHours(-5)
                },
                new LabEquipmentLog
                {
                    EquipmentName = "Beckman Coulter Allegra X-30R Refrigerated Centrifuge",
                    EquipmentModel = "Allegra X-30R",
                    SerialNumber = "BC-X30R-11409",
                    Location = "Specimen Processing / Intake",
                    LogType = "PreventiveMaintenance",
                    ControlLotNumber = null,
                    StandardDeviationScore = null,
                    Status = "Passed",
                    Findings = "Rotor bowl sanitization completed. Chamber cooling verified at 4°C. Tachometer spin audit clocked 4,000 RPM (deviation < 0.5%).",
                    PerformedByUserId = labTechId,
                    PerformedByUserName = labTechName,
                    PerformedAt = DateTime.UtcNow.AddDays(-3),
                    NextMaintenanceDueDate = DateTime.UtcNow.AddDays(27),
                    CreatedAt = DateTime.UtcNow.AddDays(-3)
                },
                new LabEquipmentLog
                {
                    EquipmentName = "Bio-Rad D-10 Hemoglobin Testing System",
                    EquipmentModel = "D-10 HPLC",
                    SerialNumber = "BRD-10-88412",
                    Location = "Special Chemistry Bench",
                    LogType = "DailyCalibration",
                    ControlLotNumber = "LYPHOCHEK-HBA1C-22",
                    StandardDeviationScore = 1.85m,
                    Status = "Warning",
                    Findings = "HbA1c retention peak time drifted +0.06 min compared to calibrator run. Column pressure at 1,420 psi.",
                    CorrectiveAction = "Performed automated high-salt buffer wash cycle. Recalibration scheduled before next batch.",
                    PerformedByUserId = labTechId,
                    PerformedByUserName = labTechName,
                    PerformedAt = DateTime.UtcNow.AddHours(-8),
                    NextMaintenanceDueDate = DateTime.UtcNow.AddDays(2),
                    CreatedAt = DateTime.UtcNow.AddHours(-8)
                }
            };
            context.LabEquipmentLogs.AddRange(logs);
            await context.SaveChangesAsync();
        }

        // 4. Seed Diverse Lab Orders if fewer than 5 exist
        var labOrderCount = await context.LabOrders.CountAsync();
        if (labOrderCount < 5)
        {
            var cbcTemplate = await context.LabTestTemplates.FirstOrDefaultAsync(t => t.TestCode == "CBC");
            var fbgTemplate = await context.LabTestTemplates.FirstOrDefaultAsync(t => t.TestCode == "FBG");
            var lipidTemplate = await context.LabTestTemplates.FirstOrDefaultAsync(t => t.TestCode == "LIPID");

            if (cbcTemplate != null && fbgTemplate != null)
            {
                // Order A: SampleCollected awaiting analyzer execution
                var orderA = new LabOrder
                {
                    OrderNumber = $"LAB-{DateTime.UtcNow:yyyyMMdd}-0101",
                    PatientId = patient.Id,
                    DoctorId = doctorId,
                    OrderDate = DateTime.UtcNow.AddHours(-2),
                    Status = LabOrderStatus.SampleCollected,
                    Priority = LabOrderPriority.Urgent,
                    ClinicalNotes = "Patient presenting with acute fatigue and pale conjunctiva. Stat CBC required.",
                    TotalCost = cbcTemplate.Price,
                    IsBilled = false,
                    SampleCollectedAt = DateTime.UtcNow.AddHours(-1),
                    CollectedByTechnicianName = labTechName,
                    SpecimenBarcode = $"SPM-{DateTime.UtcNow:yyyyMMdd}-0101",
                    SpecimenType = "Whole Blood",
                    SampleCondition = "Viable",
                    TechnicianNotes = "Sample collected via 21G butterfly needle into EDTA lavender tube. Inverted 8x."
                };
                orderA.Items.Add(new LabOrderItem
                {
                    LabTestTemplateId = cbcTemplate.Id,
                    Status = LabOrderStatus.SampleCollected,
                    Price = cbcTemplate.Price,
                    SpecimenBarcode = orderA.SpecimenBarcode
                });
                context.LabOrders.Add(orderA);

                // Order B: Ordered awaiting specimen intake
                var orderB = new LabOrder
                {
                    OrderNumber = $"LAB-{DateTime.UtcNow:yyyyMMdd}-0102",
                    PatientId = patient.Id,
                    DoctorId = doctorId,
                    OrderDate = DateTime.UtcNow.AddMinutes(-45),
                    Status = LabOrderStatus.Ordered,
                    Priority = LabOrderPriority.Routine,
                    ClinicalNotes = "Routine pre-operative workup: Fasting Blood Glucose and Lipid Profile.",
                    TotalCost = fbgTemplate.Price + (lipidTemplate?.Price ?? 35m),
                    IsBilled = false
                };
                orderB.Items.Add(new LabOrderItem
                {
                    LabTestTemplateId = fbgTemplate.Id,
                    Status = LabOrderStatus.Ordered,
                    Price = fbgTemplate.Price
                });
                if (lipidTemplate != null)
                {
                    orderB.Items.Add(new LabOrderItem
                    {
                        LabTestTemplateId = lipidTemplate.Id,
                        Status = LabOrderStatus.Ordered,
                        Price = lipidTemplate.Price
                    });
                }
                context.LabOrders.Add(orderB);

                // Order C: Completed with abnormal results and verified
                var orderC = new LabOrder
                {
                    OrderNumber = $"LAB-{DateTime.UtcNow:yyyyMMdd}-0089",
                    PatientId = patient.Id,
                    DoctorId = doctorId,
                    OrderDate = DateTime.UtcNow.AddDays(-1),
                    Status = LabOrderStatus.Completed,
                    Priority = LabOrderPriority.Routine,
                    ClinicalNotes = "Follow-up glycemic control monitoring in suspected Type 2 Diabetes.",
                    TotalCost = fbgTemplate.Price,
                    IsBilled = true,
                    SampleCollectedAt = DateTime.UtcNow.AddDays(-1).AddHours(1),
                    CollectedByTechnicianName = labTechName,
                    CompletedAt = DateTime.UtcNow.AddDays(-1).AddHours(3),
                    SpecimenBarcode = $"SPM-{DateTime.UtcNow.AddDays(-1):yyyyMMdd}-0089",
                    SpecimenType = "Fluoride Plasma",
                    SampleCondition = "Viable",
                    TechnicianNotes = "Fasting state verified > 10 hours."
                };
                var itemC = new LabOrderItem
                {
                    LabTestTemplateId = fbgTemplate.Id,
                    Status = LabOrderStatus.Completed,
                    Price = fbgTemplate.Price,
                    SpecimenBarcode = orderC.SpecimenBarcode
                };
                var resultC = new LabResult
                {
                    LabOrderItem = itemC,
                    PerformedByTechnicianId = labTechId,
                    VerifiedByDoctorId = doctorId,
                    ResultDate = DateTime.UtcNow.AddDays(-1).AddHours(3),
                    ParameterResultsJson = @"[
                        {""name"": ""Fasting Glucose"", ""value"": ""185.0"", ""unit"": ""mg/dL"", ""minRef"": 70.0, ""maxRef"": 99.0, ""flag"": ""High"", ""normalText"": ""70 - 99 mg/dL""}
                    ]",
                    Remarks = "Markedly elevated fasting plasma glucose. Repeated in duplicate with identical reading (185 mg/dL). Clinician alerted.",
                    HasAbnormalFlag = true
                };
                context.LabResults.Add(resultC);
                orderC.Items.Add(itemC);
                context.LabOrders.Add(orderC);

                // Order D: Rejected specimen with clinical reason
                var orderD = new LabOrder
                {
                    OrderNumber = $"LAB-{DateTime.UtcNow:yyyyMMdd}-0092",
                    PatientId = patient.Id,
                    DoctorId = doctorId,
                    OrderDate = DateTime.UtcNow.AddDays(-1),
                    Status = LabOrderStatus.Ordered,
                    Priority = LabOrderPriority.Stat,
                    ClinicalNotes = "Emergency department electrolyte and hematology evaluation.",
                    TotalCost = cbcTemplate.Price,
                    IsBilled = false,
                    SpecimenBarcode = $"SPM-{DateTime.UtcNow.AddDays(-1):yyyyMMdd}-0092",
                    SpecimenType = "Whole Blood",
                    SampleCondition = "Grossly Hemolyzed",
                    IsRejected = true,
                    RejectionReason = "Severe Hemolysis (Free hemoglobin index > 400 mg/dL)",
                    RejectedAt = DateTime.UtcNow.AddDays(-1).AddHours(2),
                    RejectedByTechnicianName = labTechName,
                    RequiresRecollection = true,
                    TechnicianNotes = "Severe in-vitro hemolysis prevents optical CBC differentiation and electrolyte testing. Phlebotomy nurse notified for redraw."
                };
                var itemD = new LabOrderItem
                {
                    LabTestTemplateId = cbcTemplate.Id,
                    Status = LabOrderStatus.Ordered,
                    Price = cbcTemplate.Price,
                    SpecimenBarcode = orderD.SpecimenBarcode,
                    IsRejected = true,
                    RejectionReason = "Severe Hemolysis"
                };
                orderD.Items.Add(itemD);
                context.LabOrders.Add(orderD);

                await context.SaveChangesAsync();
            }
        }

        // 5. Seed Laboratory Diagnostic Invoices if none exist
        if (!await context.Invoices.AnyAsync(i => i.Department == "Laboratory" || i.InvoiceNumber.StartsWith("INV-LAB")))
        {
            var fbgTemplate = await context.LabTestTemplates.FirstOrDefaultAsync(t => t.TestCode == "FBG");
            var cbcTemplate = await context.LabTestTemplates.FirstOrDefaultAsync(t => t.TestCode == "CBC");
            var lipidTemplate = await context.LabTestTemplates.FirstOrDefaultAsync(t => t.TestCode == "LIPID");

            var labInvoice1 = new Invoice
            {
                Id = Guid.NewGuid(),
                InvoiceNumber = $"INV-LAB-{DateTime.UtcNow:yyyyMMdd}-1042",
                PatientId = patient.Id,
                IssueDate = DateTime.UtcNow.AddDays(-1),
                DueDate = DateTime.UtcNow.AddDays(14),
                SubTotal = 35.00m,
                TaxAmount = 0m,
                DiscountAmount = 0m,
                TotalAmount = 35.00m,
                Status = InvoiceStatus.Unpaid,
                Department = "Laboratory",
                BillingNotes = "Generated for Pathology Diagnostic Lab Order LAB-2026-0089",
                LineItems = new List<InvoiceItem>
                {
                    new InvoiceItem
                    {
                        Id = Guid.NewGuid(),
                        Description = $"[FBG] {fbgTemplate?.TestName ?? "Fasting Blood Glucose"} - Plasma Analysis",
                        Quantity = 1,
                        UnitPrice = 35.00m
                    }
                }
            };

            var labInvoice2 = new Invoice
            {
                Id = Guid.NewGuid(),
                InvoiceNumber = $"INV-LAB-{DateTime.UtcNow:yyyyMMdd}-1043",
                PatientId = patient.Id,
                IssueDate = DateTime.UtcNow.AddDays(-3),
                DueDate = DateTime.UtcNow.AddDays(12),
                SubTotal = 75.00m,
                TaxAmount = 0m,
                DiscountAmount = 0m,
                TotalAmount = 75.00m,
                Status = InvoiceStatus.Paid,
                Department = "Laboratory",
                BillingNotes = "Generated for Comprehensive Diagnostic Lipid & CBC Panel",
                LineItems = new List<InvoiceItem>
                {
                    new InvoiceItem
                    {
                        Id = Guid.NewGuid(),
                        Description = $"[CBC] {cbcTemplate?.TestName ?? "Complete Blood Count"} - Hematology Differential",
                        Quantity = 1,
                        UnitPrice = 40.00m
                    },
                    new InvoiceItem
                    {
                        Id = Guid.NewGuid(),
                        Description = $"[LIPID] {lipidTemplate?.TestName ?? "Lipid Profile"} - Serum Spectrophotometry",
                        Quantity = 1,
                        UnitPrice = 35.00m
                    }
                },
                Payments = new List<Payment>
                {
                    new Payment
                    {
                        Id = Guid.NewGuid(),
                        PaymentDate = DateTime.UtcNow.AddDays(-3).AddHours(2),
                        AmountPaid = 75.00m,
                        PaymentMethod = "Cash",
                        TransactionReference = "CSH-LAB-9921"
                    }
                }
            };

            context.Invoices.AddRange(labInvoice1, labInvoice2);
            await context.SaveChangesAsync();
        }

        Console.WriteLine("✓ Seeded Laboratory Suite: reagents, requisitions, equipment logs, accessioned orders, and laboratory diagnostic invoices!");
    }

    private static async Task SeedRadiologySuiteAsync(ClinicDbContext context)
    {
        var patient = await context.Patients.FirstOrDefaultAsync(p => p.LastName == "Connor")
            ?? await context.Patients.FirstOrDefaultAsync();
        var doctor = await context.Users.FirstOrDefaultAsync(u => u.Username == "dr.smith")
            ?? await context.Users.FirstOrDefaultAsync();

        if (patient == null || doctor == null) return;

        var existingElena = await context.RadiologyOrders.FirstOrDefaultAsync(r => r.OrderNumber == "RAD-20260919-0006");
        if (existingElena != null)
        {
            existingElena.PerformedByTechnicianName = "Dr. Elena Vance, MD";
            existingElena.RadiologistSignature = "Dr. Elena Vance, MD (Diagnostic Radiologist & Neuroradiologist)";
            await context.SaveChangesAsync();
        }
        else
        {
            var elenaHasOrders = await context.RadiologyOrders.AnyAsync(r => r.RadiologistSignature != null && r.RadiologistSignature.Contains("Elena"));
            if (!elenaHasOrders)
            {
                var uniqueOrderNum = $"RAD-{DateTime.UtcNow:yyyyMMdd}-{new Random().Next(10000, 99999)}";
                var uniqueAccNum = $"ACC-{DateTime.UtcNow:yyyyMMdd}-{new Random().Next(10000, 99999)}";
                var elenaOrder = new RadiologyOrder
                {
                    Id = Guid.NewGuid(),
                    OrderNumber = uniqueOrderNum,
                    AccessionNumber = uniqueAccNum,
                    RoomSuite = "Suite 3 - MRI GE SIGNA 1.5T",
                    EquipmentModel = "GE SIGNA Creator 1.5T",
                    PatientId = patient.Id,
                    DoctorId = doctor.Id,
                    Modality = "MRI",
                    ProcedureName = "Brain MRI with & without Contrast",
                    BodyPart = "Head / Brain",
                    ClinicalIndication = "Chronic intractable migraines with visual aura. Rule out demyelinating lesions.",
                    Priority = "Urgent",
                    Status = "Completed",
                    OrderedAt = DateTime.UtcNow.AddHours(-12),
                    ScheduledAt = DateTime.UtcNow.AddHours(-10),
                    PerformedAt = DateTime.UtcNow.AddHours(-8),
                    PerformedByTechnicianName = "Dr. Elena Vance, MD",
                    Findings = "High-resolution axial T1, T2, FLAIR sequences demonstrate normal ventricular volume. No restricted diffusion on DWI. Sella turcica and cerebellopontine angles unremarkable. Clear paranasal sinuses.",
                    Impression = "Normal high-resolution brain MRI study without intracranial mass, vascular anomaly, or demyelinating plague.",
                    ImageUrl = "https://images.unsplash.com/photo-1559757175-5700dde675bc?auto=format&fit=crop&w=800&q=80",
                    RadiologistSignature = "Dr. Elena Vance, MD (Diagnostic Radiologist & Neuroradiologist)",
                    ReportedAt = DateTime.UtcNow.AddHours(-6),
                    ExposureParameters = "GE Brain 1.5T Protocol, TR 450, TE 12, Matrix 256x256",
                    ContrastUsed = true,
                    ContrastAgentName = "Gadavist (Gadobutrol)",
                    ContrastVolumeMl = 7.5m,
                    ContrastLotNumber = "LOT-GAD7719A",
                    ContrastRoute = "IV peripheral",
                    Price = 520.00m,
                    IsBilled = true
                };
                context.RadiologyOrders.Add(elenaOrder);
                await context.SaveChangesAsync();
            }
        }

        if (await context.RadiologyOrders.AnyAsync(r => r.OrderNumber == "RAD-20260919-0001")) return;

        var orders = new List<RadiologyOrder>
        {
            new RadiologyOrder
            {
                Id = Guid.NewGuid(),
                OrderNumber = "RAD-20260919-0001",
                AccessionNumber = "ACC-20260919-0001",
                RoomSuite = "Suite 1 - Digital X-Ray",
                EquipmentModel = "Siemens Multix Impact DR",
                PatientId = patient.Id,
                DoctorId = doctor.Id,
                Modality = "X-Ray",
                ProcedureName = "Chest X-Ray PA & Lateral",
                BodyPart = "Chest / Thorax",
                ClinicalIndication = "Productive cough and localized left lower lobe crackles. Rule out community-acquired pneumonia.",
                Priority = "Urgent",
                Status = "Completed",
                OrderedAt = DateTime.UtcNow.AddHours(-4),
                ScheduledAt = DateTime.UtcNow.AddHours(-3.5),
                PerformedAt = DateTime.UtcNow.AddHours(-3),
                PerformedByTechnicianName = "Dr. Ray Radiologist, MD, FACR",
                Findings = "Subsegmental consolidation and patchy airspace opacity observed in left lower lobe. Cardiothoracic silhouette within normal limits. Normal costophrenic angles. No pleural effusion or pneumothorax.",
                Impression = "Left lower lobe bronchopneumonia. Recommend clinical correlation and follow-up post antimicrobial therapy.",
                ImageUrl = "https://images.unsplash.com/photo-1516549655169-df83a0774514?auto=format&fit=crop&w=800&q=80",
                RadiologistSignature = "Dr. Ray Radiologist, MD, FACR",
                ReportedAt = DateTime.UtcNow.AddHours(-2),
                RadiationDoseDap = 0.14m,
                ExposureParameters = "115 kVp, 3.2 mAs, SID 180cm",
                IsCritical = false,
                Price = 75.00m,
                IsBilled = true
            },
            new RadiologyOrder
            {
                Id = Guid.NewGuid(),
                OrderNumber = "RAD-20260919-0002",
                AccessionNumber = "ACC-20260919-0002",
                RoomSuite = "Suite 4 - Ultrasound Bay A",
                EquipmentModel = "Philips EPIQ Elite US",
                PatientId = patient.Id,
                DoctorId = doctor.Id,
                Modality = "Ultrasound",
                ProcedureName = "Abdominal Complete Sonogram",
                BodyPart = "Abdomen",
                ClinicalIndication = "Intermittent post-prandial right upper quadrant pain. Rule out cholelithiasis.",
                Priority = "Routine",
                Status = "Completed",
                OrderedAt = DateTime.UtcNow.AddHours(-6),
                ScheduledAt = DateTime.UtcNow.AddHours(-5.5),
                PerformedAt = DateTime.UtcNow.AddHours(-5),
                PerformedByTechnicianName = "Dr. Ray Radiologist, MD, FACR",
                Findings = "Liver exhibits homogeneous parenchymal echotexture without focal mass. Gallbladder contains multiple mobile acoustic-shadowing calculi (largest 1.2 cm). Gallbladder wall normal thickness (2.1 mm). Common bile duct 4.0 mm. Spleen and pancreas unremarkable.",
                Impression = "Cholelithiasis without evidence of acute cholecystitis or biliary tree dilatation.",
                ImageUrl = "https://images.unsplash.com/photo-1579684385127-1ef15d508118?auto=format&fit=crop&w=800&q=80",
                RadiologistSignature = "Dr. Ray Radiologist, MD, FACR",
                ReportedAt = DateTime.UtcNow.AddHours(-4),
                ExposureParameters = "Curvilinear C5-1 Transducer, Frequency 3.5 MHz",
                Price = 120.00m,
                IsBilled = true
            },
            new RadiologyOrder
            {
                Id = Guid.NewGuid(),
                OrderNumber = "RAD-20260919-0003",
                AccessionNumber = "ACC-20260919-0003",
                RoomSuite = "Suite 2 - CT Suite (SOMATOM)",
                EquipmentModel = "Siemens SOMATOM go.Top (128-Slice)",
                PatientId = patient.Id,
                DoctorId = doctor.Id,
                Modality = "CT Scan",
                ProcedureName = "CT Chest with IV Contrast",
                BodyPart = "Chest",
                ClinicalIndication = "Acute pleuritic chest pain and dyspnea. Suspected pulmonary embolism or aortic pathology.",
                Priority = "Stat",
                Status = "Completed",
                OrderedAt = DateTime.UtcNow.AddHours(-3),
                ScheduledAt = DateTime.UtcNow.AddHours(-2.5),
                PerformedAt = DateTime.UtcNow.AddHours(-2),
                PerformedByTechnicianName = "Dr. Ray Radiologist, MD, FACR",
                Findings = "CRITICAL FINDING: Large filling defect identified in the right main pulmonary artery extending into the right interlobar and lower lobe segmental branches, consistent with acute pulmonary embolism. Right ventricular strain noted with RV/LV ratio of 1.3.",
                Impression = "EMERGENCY: Acute right pulmonary thromboembolism with evidence of right heart strain. Ordering physician notified verbally at 14:15 by Dr. Ray Radiologist.",
                ImageUrl = "https://images.unsplash.com/photo-1530497610245-94d3c16cda28?auto=format&fit=crop&w=800&q=80",
                RadiologistSignature = "Dr. Ray Radiologist, MD, FACR",
                ReportedAt = DateTime.UtcNow.AddHours(-1.5),
                IsCritical = true,
                CriticalAlertNotes = "EMERGENCY: Acute right main pulmonary embolism with RV strain. Immediate anticoagulation recommended.",
                CriticalAlertAcknowledgedByDoctor = false,
                ContrastUsed = true,
                ContrastAgentName = "Omnipaque 350 (Iohexol)",
                ContrastVolumeMl = 85.0m,
                ContrastLotNumber = "LOT-OMN8921B",
                ContrastRoute = "IV via 18G antecubital cannula",
                RadiationDoseDlp = 420.5m,
                RadiationDoseDap = 18.2m,
                ExposureParameters = "120 kVp, 260 mAs, 0.6mm slice thickness, Pitch 0.9",
                Price = 280.00m,
                IsBilled = true
            },
            new RadiologyOrder
            {
                Id = Guid.NewGuid(),
                OrderNumber = "RAD-20260919-0004",
                AccessionNumber = "ACC-20260919-0004",
                RoomSuite = "Suite 3 - MRI GE SIGNA 1.5T",
                EquipmentModel = "GE SIGNA Creator 1.5T",
                PatientId = patient.Id,
                DoctorId = doctor.Id,
                Modality = "MRI",
                ProcedureName = "Lumbar Spine MRI Non-Contrast",
                BodyPart = "Spine / Lumbar",
                ClinicalIndication = "Persistent lower back pain radiating down right L5 dermatome.",
                Priority = "Routine",
                Status = "Scheduled",
                OrderedAt = DateTime.UtcNow.AddHours(-2),
                ScheduledAt = DateTime.UtcNow.AddHours(2),
                Price = 480.00m,
                IsBilled = false
            },
            new RadiologyOrder
            {
                Id = Guid.NewGuid(),
                OrderNumber = "RAD-20260919-0005",
                AccessionNumber = "ACC-20260919-0005",
                RoomSuite = "Suite 1 - Digital X-Ray",
                EquipmentModel = "Siemens Multix Impact DR",
                PatientId = patient.Id,
                DoctorId = doctor.Id,
                Modality = "X-Ray",
                ProcedureName = "Right Wrist AP, Lateral & Oblique",
                BodyPart = "Right Wrist",
                ClinicalIndication = "Fall onto outstretched hand (FOOSH). Significant swelling over anatomical snuffbox.",
                Priority = "Urgent",
                Status = "Ordered",
                OrderedAt = DateTime.UtcNow.AddMinutes(-20),
                Price = 70.00m,
                IsBilled = false
            }
        };

        context.RadiologyOrders.AddRange(orders);
        await context.SaveChangesAsync();

        // Seed Alice Patient Radiology Orders if not present
        var alice = await context.Patients.FirstOrDefaultAsync(p => p.MedicalRecordNumber == "PAT-ALICE-001" || p.Email == "alice.patient@clinic.com");
        if (alice != null && !await context.RadiologyOrders.AnyAsync(r => r.PatientId == alice.Id))
        {
            var aliceOrders = new List<RadiologyOrder>
            {
                new RadiologyOrder
                {
                    Id = Guid.NewGuid(),
                    OrderNumber = $"RAD-{DateTime.UtcNow:yyyyMMdd}-0011",
                    AccessionNumber = $"ACC-{DateTime.UtcNow:yyyyMMdd}-0011",
                    RoomSuite = "Suite 1 - Digital X-Ray",
                    EquipmentModel = "Siemens Multix Impact DR",
                    PatientId = alice.Id,
                    DoctorId = doctor.Id,
                    Modality = "X-Ray",
                    ProcedureName = "Chest X-Ray PA & Lateral",
                    BodyPart = "Chest / Thorax",
                    ClinicalIndication = "Persistent dry cough following upper respiratory viral episode. Rule out infiltrates.",
                    Priority = "Routine",
                    Status = "Completed",
                    OrderedAt = DateTime.UtcNow.AddDays(-2),
                    ScheduledAt = DateTime.UtcNow.AddDays(-2).AddHours(1),
                    PerformedAt = DateTime.UtcNow.AddDays(-2).AddHours(2),
                    PerformedByTechnicianName = "Dr. Ray Radiologist, MD, FACR",
                    Findings = "Bilateral lung fields are clear with normal vascular markings. Trachea midline. Costophrenic and cardiophrenic angles sharp. Normal cardiothoracic ratio. Unremarkable bony thorax.",
                    Impression = "Normal chest radiograph with no acute cardiopulmonary infiltrates or pleural disease.",
                    ImageUrl = "https://images.unsplash.com/photo-1516549655169-df83a0774514?auto=format&fit=crop&w=800&q=80",
                    RadiologistSignature = "Dr. Ray Radiologist, MD, FACR",
                    ReportedAt = DateTime.UtcNow.AddDays(-2).AddHours(3),
                    RadiationDoseDap = 0.12m,
                    ExposureParameters = "110 kVp, 2.8 mAs, SID 180cm",
                    Price = 75.00m,
                    IsBilled = true
                },
                new RadiologyOrder
                {
                    Id = Guid.NewGuid(),
                    OrderNumber = $"RAD-{DateTime.UtcNow:yyyyMMdd}-0012",
                    AccessionNumber = $"ACC-{DateTime.UtcNow:yyyyMMdd}-0012",
                    RoomSuite = "Suite 4 - Ultrasound Bay A",
                    EquipmentModel = "Philips EPIQ Elite US",
                    PatientId = alice.Id,
                    DoctorId = doctor.Id,
                    Modality = "Ultrasound",
                    ProcedureName = "Thyroid High-Resolution Ultrasound",
                    BodyPart = "Neck / Thyroid",
                    ClinicalIndication = "Routine endocrine follow-up and neck palpation check.",
                    Priority = "Routine",
                    Status = "Completed",
                    OrderedAt = DateTime.UtcNow.AddDays(-1),
                    ScheduledAt = DateTime.UtcNow.AddDays(-1).AddHours(1),
                    PerformedAt = DateTime.UtcNow.AddDays(-1).AddHours(2),
                    PerformedByTechnicianName = "Dr. Elena Vance, MD",
                    Findings = "Thyroid gland is normal in size and demonstrates symmetric homogeneous echotexture. Right lobe: 4.2 x 1.4 x 1.2 cm; Left lobe: 4.1 x 1.3 x 1.2 cm. Isthmus measures 2.1 mm. No cystic or solid nodular lesions. Normal cervical vascularity.",
                    Impression = "Normal thyroid and soft tissue neck ultrasound examination (ACR TI-RADS 1).",
                    ImageUrl = "https://images.unsplash.com/photo-1579684385127-1ef15d508118?auto=format&fit=crop&w=800&q=80",
                    RadiologistSignature = "Dr. Elena Vance, MD (Diagnostic Radiologist & Neuroradiologist)",
                    ReportedAt = DateTime.UtcNow.AddDays(-1).AddHours(3),
                    ExposureParameters = "Linear L12-3 Broadband Transducer, 10.0 MHz",
                    Price = 110.00m,
                    IsBilled = false
                }
            };
            context.RadiologyOrders.AddRange(aliceOrders);
            await context.SaveChangesAsync();
        }

        // Seed Radiology Diagnostic Invoices if none exist
        if (!await context.Invoices.AnyAsync(i => i.Department == "Radiology" || i.InvoiceNumber.StartsWith("INV-RAD")))
        {
            var targetPatient = alice ?? patient;
            var radInvoice1 = new Invoice
            {
                Id = Guid.NewGuid(),
                InvoiceNumber = $"INV-RAD-{DateTime.UtcNow:yyyyMMdd}-2001",
                PatientId = targetPatient.Id,
                IssueDate = DateTime.UtcNow.AddDays(-2),
                DueDate = DateTime.UtcNow.AddDays(14),
                SubTotal = 75.00m,
                TaxAmount = 0m,
                DiscountAmount = 0m,
                TotalAmount = 75.00m,
                Status = InvoiceStatus.Paid,
                Department = "Radiology",
                BillingNotes = "Diagnostic Chest X-Ray PA & Lateral study",
                LineItems = new List<InvoiceItem>
                {
                    new InvoiceItem
                    {
                        Id = Guid.NewGuid(),
                        Description = "[X-Ray] Chest X-Ray PA & Lateral (Digital Radiography)",
                        Quantity = 1,
                        UnitPrice = 75.00m
                    }
                },
                Payments = new List<Payment>
                {
                    new Payment
                    {
                        Id = Guid.NewGuid(),
                        PaymentDate = DateTime.UtcNow.AddDays(-2).AddHours(3),
                        AmountPaid = 75.00m,
                        PaymentMethod = "CreditCard",
                        TransactionReference = "TXN-RAD-88190"
                    }
                }
            };

            var radInvoice2 = new Invoice
            {
                Id = Guid.NewGuid(),
                InvoiceNumber = $"INV-RAD-{DateTime.UtcNow:yyyyMMdd}-2002",
                PatientId = targetPatient.Id,
                IssueDate = DateTime.UtcNow.AddDays(-1),
                DueDate = DateTime.UtcNow.AddDays(30),
                SubTotal = 280.00m,
                TaxAmount = 0m,
                DiscountAmount = 0m,
                TotalAmount = 280.00m,
                Status = InvoiceStatus.Unpaid,
                Department = "Radiology",
                BillingNotes = "High-Resolution CT Chest with IV Contrast",
                LineItems = new List<InvoiceItem>
                {
                    new InvoiceItem
                    {
                        Id = Guid.NewGuid(),
                        Description = "[CT Scan] CT Chest with IV Contrast (128-Slice)",
                        Quantity = 1,
                        UnitPrice = 280.00m
                    }
                }
            };

            context.Invoices.AddRange(radInvoice1, radInvoice2);
            await context.SaveChangesAsync();
        }

        Console.WriteLine("✓ Seeded Radiology Suite: diagnostic imaging studies, X-Ray reports, and sonograms!");
    }

    private static async Task SeedAllPatientComprehensiveRecordsAsync(ClinicDbContext context)
    {
        var doctor = await context.Users.FirstOrDefaultAsync(u => u.Username == "dr.smith");
        var doctorId = doctor?.Id ?? Guid.NewGuid();
        var nurse = await context.Users.FirstOrDefaultAsync(u => u.Username == "nurse.joy");
        var nurseId = nurse?.Id ?? doctorId;
        var labTech = await context.Users.FirstOrDefaultAsync(u => u.Username == "lab.tech");
        var labTechId = labTech?.Id ?? doctorId;

        var uploadsRoot = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
        if (!Directory.Exists(uploadsRoot)) Directory.CreateDirectory(uploadsRoot);

        var patients = await context.Patients.ToListAsync();
        var templates = await context.LabTestTemplates.ToListAsync();
        var lipidTemplate = templates.FirstOrDefault(t => t.TestCode == "LIPID") ?? templates.FirstOrDefault();
        var amloMed = await context.Medicines.FirstOrDefaultAsync(m => m.Name.Contains("Amlodipine"));
        var amoxMed = await context.Medicines.FirstOrDefaultAsync(m => m.Name.Contains("Amoxicillin"));
        var paraMed = await context.Medicines.FirstOrDefaultAsync(m => m.Name.Contains("Paracetamol"));
        var defaultMed = amloMed ?? amoxMed ?? paraMed;

        // 1. Patient Demographics, Contact, Insurance, and Attached Files
        foreach (var p in patients)
        {
            if (p.LastName == "Connor" || p.FirstName == "Sarah")
            {
                p.Phone = "+1 (555) 234-5678";
                p.Email = "sarah.connor@sky.net";
                p.Address = "1424 Cyberdyne Way, Los Angeles, CA";
                p.BloodGroup = "O-";
                p.EmergencyContact = "John Connor (Son) (+1 555-777-9999)";
                p.Allergies = "Penicillin, Cephalosporins (Severe Hives)";
                p.NationalId = "US-CA-9842188";
                p.InsuranceProvider = "BlueCross BlueShield";
                p.InsurancePolicyNumber = "BCBS-CA-992348";
                p.InsuranceCoveragePlan = "Platinum Comprehensive PPO";
                p.InsuranceEligibilityStatus = "Active / Eligible";
            }
            else if (p.LastName == "Beg" || p.FirstName == "Tsinat")
            {
                p.Phone = "+251 98 754 9634";
                p.Email = "tsinat.beg@clinic.et";
                p.Address = "Bole Subcity, Woreda 03, House 712, Addis Ababa";
                p.BloodGroup = "B+";
                p.EmergencyContact = "Baba Beg (Father) (+251 98 754 8727)";
                p.Allergies = "Amoxicillin (Mild Erythema)";
                p.NationalId = "ET-AA-5724910";
                p.InsuranceProvider = "Aetna International";
                p.InsurancePolicyNumber = "AET-GL-334190";
                p.InsuranceCoveragePlan = "Premier Family Gold Plan";
                p.InsuranceEligibilityStatus = "Active / Eligible";
            }
            else if (p.FirstName == "eyerus" || p.LastName == "birhanu" || p.FirstName == "Eyerus")
            {
                p.FirstName = "Eyerus";
                p.LastName = "Birhanu";
                p.Phone = "+251 95 398 4563";
                p.Email = "eyerus.birhanu@health.et";
                p.Address = "Arada Subcity, Kebele 11/12, House 501, Addis Ababa";
                p.BloodGroup = "AB+";
                p.EmergencyContact = "Tsinat Birhanu (Mother) (+251 99 845 6324)";
                p.Allergies = "Peanuts (Anaphylaxis Risk), Codeine";
                p.NationalId = "ET-AA-6415122";
                p.InsuranceProvider = "MetLife Global";
                p.InsurancePolicyNumber = "MET-INT-192840";
                p.InsuranceCoveragePlan = "Corporate Executive Health Shield";
                p.InsuranceEligibilityStatus = "Active / Eligible";
            }
            else if (p.FirstName == "omar" || p.LastName == "boss" || p.FirstName == "Omar")
            {
                p.FirstName = "Omar";
                p.LastName = "Boss";
                p.Phone = "+251 97 656 4325";
                p.Email = "omar.boss@medicare.et";
                p.Address = "Kirkos Subcity, Meskel Flower Road, House 204, Addis Ababa";
                p.BloodGroup = "A+";
                p.EmergencyContact = "Yesuf Boss (Father) (+251 98 754 3456)";
                p.Allergies = "NKDA (No Known Drug Allergies)";
                p.NationalId = "ET-AA-1475489";
                p.InsuranceProvider = "Cigna Global Health";
                p.InsurancePolicyNumber = "CIG-AF-882194";
                p.InsuranceCoveragePlan = "Gold Pediatric & Family Shield";
                p.InsuranceEligibilityStatus = "Active / Eligible";
            }
            else if (p.FirstName == "ABEBA" || p.LastName == "GASHAW" || p.FirstName == "Abeba")
            {
                p.FirstName = "Abeba";
                p.LastName = "Gashaw";
                p.Phone = "+251 99 856 7325";
                p.Email = "abeba.gashaw@telecom.et";
                p.Address = "Yeka Subcity, Signal Area, Villa 45, Addis Ababa";
                p.BloodGroup = "O-";
                p.EmergencyContact = "Kebede Gashaw (Brother) (+251 99 835 2761)";
                p.Allergies = "Sulfonamides (Bactrim)";
                p.NationalId = "ET-AA-6517409";
                p.InsuranceProvider = "UnitedHealthcare Global";
                p.InsurancePolicyNumber = "UHC-INT-541298";
                p.InsuranceCoveragePlan = "Choice Plus Comprehensive";
                p.InsuranceEligibilityStatus = "Active / Eligible";
            }
            else if (p.LastName == "Tasew" || p.FirstName == "Merdi")
            {
                p.Phone = "+251 98 725 1763";
                p.Email = "merdi.tasew@tech.et";
                p.Address = "Lemi Kura Subcity, Woreda 08, House 310, Addis Ababa";
                p.BloodGroup = "B-";
                p.EmergencyContact = "Tasew Merdi (Father) (+251 97 634 2615)";
                p.Allergies = "NKDA (No Known Drug Allergies)";
                p.NationalId = "ET-AA-9346711";
                p.InsuranceProvider = "Kaiser Permanente";
                p.InsurancePolicyNumber = "KP-US-771829";
                p.InsuranceCoveragePlan = "Signature Comprehensive HMO";
                p.InsuranceEligibilityStatus = "Active / Eligible";
            }
            else if (p.FirstName == "Alice")
            {
                p.Phone = "+1 (555) 666-7777";
                p.Email = "alice.patient@clinic.com";
                p.Address = "452 Medical Center Blvd, Suite 200, Boston, MA";
                p.BloodGroup = "O+";
                p.EmergencyContact = "Bob Patient (Spouse) (+1 555-999-8888)";
                p.Allergies = "Latex, Codeine Phosphate";
                p.NationalId = "US-MA-1029481";
                p.InsuranceProvider = "BlueCross BlueShield";
                p.InsurancePolicyNumber = "BCBS-MA-884129";
                p.InsuranceCoveragePlan = "Federal Employee Health Benefit (FEHB)";
                p.InsuranceEligibilityStatus = "Active / Eligible";
            }
            else if (p.FirstName.Contains("Walk-in"))
            {
                p.Phone = "+1 (555) 000-9999";
                p.Email = "walkin.desk@clinic.com";
                p.Address = "Outpatient Triage Reception, Station 1A";
                p.BloodGroup = "O+";
                p.EmergencyContact = "Clinic Emergency Desk (+1 555-000-0000)";
                p.Allergies = "None Disclosed (Intake NKDA)";
                p.NationalId = "TRIAGE-TEMP-001";
                p.InsuranceProvider = "Self-Pay Direct Desk";
                p.InsurancePolicyNumber = "CASH-DESK-01";
                p.InsuranceCoveragePlan = "Direct Self-Pay Consultation";
                p.InsuranceEligibilityStatus = "Self-Pay";
            }

            var patientUploadDir = Path.Combine(uploadsRoot, p.Id.ToString());
            if (!Directory.Exists(patientUploadDir)) Directory.CreateDirectory(patientUploadDir);

            var existingFiles = Directory.GetFiles(patientUploadDir);
            if (existingFiles.Length == 0)
            {
                var doc1 = Path.Combine(patientUploadDir, $"{Guid.NewGuid()}__Comprehensive-Clinical-Summary.pdf");
                var doc2 = Path.Combine(patientUploadDir, $"{Guid.NewGuid()}__Diagnostic-Laboratory-Report.pdf");
                File.WriteAllText(doc1, $"%PDF-1.4 Official Clinical Health Summary for {p.FirstName} {p.LastName} (MRN: {p.MedicalRecordNumber}). Issued by Department of Medicine.");
                File.WriteAllText(doc2, $"%PDF-1.4 Clinical Diagnostic Workup & Verified Laboratory Pathology Panel for {p.FirstName} {p.LastName}.");
            }
        }
        await context.SaveChangesAsync();

        // 2. Ensure each patient has completed consultation appointments with vitals & prescriptions
        foreach (var p in patients)
        {
            var hasCompletedAppt = await context.Appointments.AnyAsync(a => a.PatientId == p.Id && a.Status == AppointmentStatus.Completed);
            if (!hasCompletedAppt)
            {
                var appt = new Appointment
                {
                    Id = Guid.NewGuid(),
                    PatientId = p.Id,
                    DoctorId = doctorId,
                    ScheduledDateTime = DateTime.UtcNow.AddDays(-7),
                    DurationMinutes = 30,
                    Status = AppointmentStatus.Completed,
                    ReasonForVisit = p.FirstName == "Sarah" 
                        ? "Cardiology Consultation & Comprehensive Health Evaluation"
                        : "Annual Clinical Physical & Preventative Health Screening",
                    VisitType = "General Consultation",
                    RoomAssignment = "Consultation Suite 3B",
                    TriageUrgency = "Routine",
                    TriageNotes = "Intake vitals recorded. Patient alert and oriented x 3."
                };
                context.Appointments.Add(appt);
                await context.SaveChangesAsync();

                var vitals = new VitalSign
                {
                    Id = Guid.NewGuid(),
                    AppointmentId = appt.Id,
                    RecordedByUserId = nurseId,
                    SystolicBP = p.FirstName == "Sarah" ? 118 : 120,
                    DiastolicBP = p.FirstName == "Sarah" ? 76 : 80,
                    HeartRateBpm = p.FirstName == "Sarah" ? 68 : 72,
                    TemperatureC = 36.8m,
                    RespiratoryRate = 16,
                    WeightKg = p.FirstName == "Sarah" ? 62.5m : 70.0m,
                    HeightCm = 170.0m,
                    OxygenSaturation = 99,
                    RecordedAt = DateTime.UtcNow.AddDays(-7)
                };
                context.VitalSigns.Add(vitals);

                var consult = new Consultation
                {
                    Id = Guid.NewGuid(),
                    AppointmentId = appt.Id,
                    DoctorId = doctorId,
                    ConsultedAt = DateTime.UtcNow.AddDays(-7),
                    Symptoms = p.FirstName == "Sarah"
                        ? "Intermittent palpitations and mild exertional fatigue over past two weeks."
                        : "Routine health checkup, mild seasonal fatigue, no acute distress.",
                    Findings = p.FirstName == "Sarah"
                        ? "Regular S1/S2 heart sounds, no murmurs or gallops. Lungs clear to auscultation bilaterally. Peripheral pulses intact (+2/2)."
                        : "HEENT normal, chest clear, abdomen soft and non-tender, physiological examination unremarkable.",
                    Diagnosis = p.FirstName == "Sarah"
                        ? "[I49.8] Benign Supraventricular Extrasystoles / Stress-Induced Palpitations"
                        : "[Z00.00] General Adult Medical Examination Without Abnormal Findings",
                    ClinicalNotes = "Patient educated on balanced hydration, stress reduction, and scheduled for routine follow-up in 6 months."
                };
                context.Consultations.Add(consult);

                if (defaultMed != null)
                {
                    var rx = new Prescription
                    {
                        Id = Guid.NewGuid(),
                        ConsultationId = consult.Id,
                        IssuedAt = DateTime.UtcNow.AddDays(-7),
                        Status = "Dispensed",
                        Notes = "Take with food and full glass of water. Report any adverse reactions immediately."
                    };
                    context.Prescriptions.Add(rx);

                    var rxItem = new PrescriptionItem
                    {
                        Id = Guid.NewGuid(),
                        PrescriptionId = rx.Id,
                        MedicineId = defaultMed.Id,
                        Quantity = 30,
                        UnitPrice = defaultMed.UnitPrice,
                        DosageInstructions = "1 tablet orally daily in the morning after meals",
                        Status = "Dispensed",
                        DispensedQuantity = 30
                    };
                    context.PrescriptionItems.Add(rxItem);
                }
                await context.SaveChangesAsync();
            }
        }

        // 3. Ensure each patient has at least one Diagnostic Lab Order with Results
        foreach (var p in patients)
        {
            var hasLabOrder = await context.LabOrders.AnyAsync(lo => lo.PatientId == p.Id);
            if (!hasLabOrder && lipidTemplate != null)
            {
                var labOrder = new LabOrder
                {
                    Id = Guid.NewGuid(),
                    OrderNumber = $"LAB-{DateTime.UtcNow:yyyyMMdd}-{new Random().Next(1000, 9999)}",
                    PatientId = p.Id,
                    DoctorId = doctorId,
                    OrderDate = DateTime.UtcNow.AddDays(-5),
                    Status = LabOrderStatus.Completed,
                    Priority = LabOrderPriority.Routine,
                    ClinicalNotes = "Standard baseline diagnostic panel ordered during routine clinical physical.",
                    TotalCost = lipidTemplate.Price,
                    IsBilled = true,
                    SampleCollectedAt = DateTime.UtcNow.AddDays(-5).AddHours(1),
                    CollectedByTechnicianName = "Leo Technician",
                    SpecimenBarcode = $"SPEC-{DateTime.UtcNow:yyMM}-{new Random().Next(10000, 99999)}",
                    SpecimenType = lipidTemplate.SampleType ?? "Serum",
                    SampleCondition = "Viable",
                    CompletedAt = DateTime.UtcNow.AddDays(-5).AddHours(4)
                };
                context.LabOrders.Add(labOrder);

                var orderItem = new LabOrderItem
                {
                    Id = Guid.NewGuid(),
                    LabOrderId = labOrder.Id,
                    LabTestTemplateId = lipidTemplate.Id,
                    Status = LabOrderStatus.Completed,
                    Price = lipidTemplate.Price,
                    SpecimenBarcode = labOrder.SpecimenBarcode
                };
                context.LabOrderItems.Add(orderItem);

                var paramResults = "[{\"name\":\"Total Cholesterol\",\"value\":\"178\",\"unit\":\"mg/dL\",\"normalText\":\"< 200 mg/dL\",\"flag\":\"Normal\"},{\"name\":\"HDL Cholesterol\",\"value\":\"58\",\"unit\":\"mg/dL\",\"normalText\":\"> 45 mg/dL\",\"flag\":\"Normal\"},{\"name\":\"LDL Cholesterol\",\"value\":\"102\",\"unit\":\"mg/dL\",\"normalText\":\"< 130 mg/dL\",\"flag\":\"Normal\"},{\"name\":\"Triglycerides\",\"value\":\"110\",\"unit\":\"mg/dL\",\"normalText\":\"< 150 mg/dL\",\"flag\":\"Normal\"}]";

                var result = new LabResult
                {
                    Id = Guid.NewGuid(),
                    LabOrderItemId = orderItem.Id,
                    PerformedByTechnicianId = labTechId,
                    VerifiedByDoctorId = doctorId,
                    ResultDate = DateTime.UtcNow.AddDays(-5).AddHours(4),
                    ParameterResultsJson = paramResults,
                    Remarks = "Lipid panel values are within normal reference limits. Low cardiovascular risk profile.",
                    HasAbnormalFlag = false
                };
                context.LabResults.Add(result);
                await context.SaveChangesAsync();
            }
        }

        // 4. Ensure each patient has at least one Invoice
        foreach (var p in patients)
        {
            var hasInvoice = await context.Invoices.AnyAsync(i => i.PatientId == p.Id);
            if (!hasInvoice)
            {
                var inv = new Invoice
                {
                    Id = Guid.NewGuid(),
                    InvoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMdd}-{new Random().Next(1000, 9999)}",
                    PatientId = p.Id,
                    IssueDate = DateTime.UtcNow.AddDays(-5),
                    DueDate = DateTime.UtcNow.AddDays(25),
                    Status = InvoiceStatus.Paid,
                    Department = "Consultation",
                    SubTotal = 95.00m,
                    TaxAmount = 0.00m,
                    DiscountAmount = 0.00m,
                    TotalAmount = 95.00m,
                    BillingNotes = "Consultation and baseline diagnostic laboratory fees settled."
                };
                context.Invoices.Add(inv);

                var item1 = new InvoiceItem
                {
                    Id = Guid.NewGuid(),
                    InvoiceId = inv.Id,
                    Description = "General Physician Comprehensive Consultation",
                    Quantity = 1,
                    UnitPrice = 50.00m
                };
                var item2 = new InvoiceItem
                {
                    Id = Guid.NewGuid(),
                    InvoiceId = inv.Id,
                    Description = "Clinical Diagnostic Laboratory Testing",
                    Quantity = 1,
                    UnitPrice = 45.00m
                };
                context.InvoiceItems.AddRange(item1, item2);

                var payment = new Payment
                {
                    Id = Guid.NewGuid(),
                    InvoiceId = inv.Id,
                    AmountPaid = 95.00m,
                    PaymentDate = DateTime.UtcNow.AddDays(-5),
                    PaymentMethod = "CreditCard",
                    TransactionReference = $"TXN-CC-{new Random().Next(100000, 999999)}"
                };
                context.Payments.Add(payment);
                await context.SaveChangesAsync();
            }
        }

        Console.WriteLine("✓ Comprehensive Patient EMR Data Seeding completed: All 8 patients updated with rich demographics, consultations, lab results, invoices, and documents!");
    }

    private static async Task SeedMultiDoctorAppointmentsAsync(ClinicDbContext context)
    {
        var docSmith = await context.Users.FirstOrDefaultAsync(u => u.Username == "dr.smith");
        var docJames = await context.Users.FirstOrDefaultAsync(u => u.Username == "dr.james");
        var nurse = await context.Users.FirstOrDefaultAsync(u => u.Username == "nurse.joy");
        var nurseId = nurse?.Id ?? Guid.NewGuid();

        if (docSmith == null || docJames == null) return;

        var allPatients = await context.Patients.ToListAsync();

        var smithPatients = allPatients.Where(p => 
            p.LastName == "Connor" || p.FirstName == "Alice" || p.LastName == "Boss" || p.LastName == "Birhanu"
        ).ToList();

        var jamesPatients = allPatients.Where(p => 
            p.LastName == "Beg" || p.LastName == "Tasew" || p.FirstName.Contains("Walk-in") || p.LastName == "Gashaw"
        ).ToList();

        // 1. Reassign past appointments and consultations to enforce clear isolation
        foreach (var p in jamesPatients)
        {
            var appts = await context.Appointments.Where(a => a.PatientId == p.Id).ToListAsync();
            foreach (var a in appts)
            {
                a.DoctorId = docJames.Id;
            }
            var apptIds = appts.Select(a => a.Id).ToList();
            var consults = await context.Consultations
                .Where(c => apptIds.Contains(c.AppointmentId))
                .ToListAsync();
            foreach (var c in consults)
            {
                c.DoctorId = docJames.Id;
            }
        }

        foreach (var p in smithPatients)
        {
            var appts = await context.Appointments.Where(a => a.PatientId == p.Id).ToListAsync();
            foreach (var a in appts)
            {
                a.DoctorId = docSmith.Id;
            }
            var apptIds = appts.Select(a => a.Id).ToList();
            var consults = await context.Consultations
                .Where(c => apptIds.Contains(c.AppointmentId))
                .ToListAsync();
            foreach (var c in consults)
            {
                c.DoctorId = docSmith.Id;
            }
        }
        await context.SaveChangesAsync();

        // 2. Ensure today's active queues for both doctors
        var today = DateTime.UtcNow.Date;
        var hasActiveTodayAppts = await context.Appointments.AnyAsync(a => a.ScheduledDateTime.Date == today && a.Status != AppointmentStatus.Completed);

        if (!hasActiveTodayAppts)
        {
            var newAppts = new List<Appointment>();
            var newVitals = new List<VitalSign>();

            // For Dr. Smith:
            var alice = smithPatients.FirstOrDefault(p => p.FirstName == "Alice");
            if (alice != null)
            {
                var apptAlice = new Appointment
                {
                    Id = Guid.NewGuid(),
                    PatientId = alice.Id,
                    DoctorId = docSmith.Id,
                    ScheduledDateTime = today.AddHours(9).AddMinutes(30),
                    DurationMinutes = 30,
                    Status = AppointmentStatus.CheckedIn,
                    ReasonForVisit = "Allergic Rhinitis Follow-Up & Nasal Assessment",
                    VisitType = "Follow-up",
                    RoomAssignment = "Consultation Suite 3A",
                    TokenNumber = "Q-101",
                    TriageUrgency = "Routine",
                    TriageNotes = "Patient in waiting room. Intake vitals normal."
                };
                newAppts.Add(apptAlice);
                newVitals.Add(new VitalSign
                {
                    Id = Guid.NewGuid(),
                    AppointmentId = apptAlice.Id,
                    RecordedByUserId = nurseId,
                    SystolicBP = 118,
                    DiastolicBP = 76,
                    HeartRateBpm = 72,
                    TemperatureC = 36.6m,
                    RespiratoryRate = 16,
                    WeightKg = 64.0m,
                    HeightCm = 168.0m,
                    OxygenSaturation = 99,
                    RecordedAt = today.AddHours(9).AddMinutes(15)
                });
            }

            var omar = smithPatients.FirstOrDefault(p => p.LastName == "Boss");
            if (omar != null)
            {
                newAppts.Add(new Appointment
                {
                    Id = Guid.NewGuid(),
                    PatientId = omar.Id,
                    DoctorId = docSmith.Id,
                    ScheduledDateTime = today.AddHours(11).AddMinutes(0),
                    DurationMinutes = 30,
                    Status = AppointmentStatus.Scheduled,
                    ReasonForVisit = "Hypertension Screening & Routine Physical",
                    VisitType = "General Consultation",
                    RoomAssignment = "Consultation Suite 3A",
                    TokenNumber = "Q-102",
                    TriageUrgency = "Routine"
                });
            }

            // For Dr. Sarah James:
            var tsinat = jamesPatients.FirstOrDefault(p => p.LastName == "Beg");
            if (tsinat != null)
            {
                var apptTsinat = new Appointment
                {
                    Id = Guid.NewGuid(),
                    PatientId = tsinat.Id,
                    DoctorId = docJames.Id,
                    ScheduledDateTime = today.AddHours(14).AddMinutes(0),
                    DurationMinutes = 30,
                    Status = AppointmentStatus.CheckedIn,
                    ReasonForVisit = "Pediatric Wellness Exam & Vaccine Review",
                    VisitType = "Pediatric Care",
                    RoomAssignment = "Pediatrics Suite 102",
                    TokenNumber = "P-201",
                    TriageUrgency = "Routine",
                    TriageNotes = "Vitals recorded. Patient and mother present in waiting room."
                };
                newAppts.Add(apptTsinat);
                newVitals.Add(new VitalSign
                {
                    Id = Guid.NewGuid(),
                    AppointmentId = apptTsinat.Id,
                    RecordedByUserId = nurseId,
                    SystolicBP = 110,
                    DiastolicBP = 70,
                    HeartRateBpm = 80,
                    TemperatureC = 36.7m,
                    RespiratoryRate = 18,
                    WeightKg = 52.0m,
                    HeightCm = 158.0m,
                    OxygenSaturation = 100,
                    RecordedAt = today.AddHours(13).AddMinutes(45)
                });
            }

            var merdi = jamesPatients.FirstOrDefault(p => p.LastName == "Tasew");
            if (merdi != null)
            {
                newAppts.Add(new Appointment
                {
                    Id = Guid.NewGuid(),
                    PatientId = merdi.Id,
                    DoctorId = docJames.Id,
                    ScheduledDateTime = today.AddHours(15).AddMinutes(30),
                    DurationMinutes = 30,
                    Status = AppointmentStatus.Scheduled,
                    ReasonForVisit = "Persistent Cough & Bronchial Discomfort",
                    VisitType = "General Consultation",
                    RoomAssignment = "Pediatrics Suite 102",
                    TokenNumber = "P-202",
                    TriageUrgency = "Urgent"
                });
            }

            var walkin = jamesPatients.FirstOrDefault(p => p.FirstName.Contains("Walk-in"));
            if (walkin != null)
            {
                var apptWalkin = new Appointment
                {
                    Id = Guid.NewGuid(),
                    PatientId = walkin.Id,
                    DoctorId = docJames.Id,
                    ScheduledDateTime = today.AddHours(16).AddMinutes(0),
                    DurationMinutes = 30,
                    Status = AppointmentStatus.CheckedIn,
                    ReasonForVisit = "Acute Skin Rash & Mild Itching",
                    VisitType = "Walk-in Triage",
                    RoomAssignment = "Triage Bay 1A",
                    TokenNumber = "W-301",
                    TriageUrgency = "Routine",
                    TriageNotes = "Self-pay walk-in registered at reception."
                };
                newAppts.Add(apptWalkin);
                newVitals.Add(new VitalSign
                {
                    Id = Guid.NewGuid(),
                    AppointmentId = apptWalkin.Id,
                    RecordedByUserId = nurseId,
                    SystolicBP = 122,
                    DiastolicBP = 78,
                    HeartRateBpm = 75,
                    TemperatureC = 36.9m,
                    RespiratoryRate = 16,
                    WeightKg = 72.0m,
                    HeightCm = 175.0m,
                    OxygenSaturation = 98,
                    RecordedAt = today.AddHours(15).AddMinutes(50)
                });
            }

            if (newAppts.Any())
            {
                context.Appointments.AddRange(newAppts);
                context.VitalSigns.AddRange(newVitals);
                await context.SaveChangesAsync();
                Console.WriteLine("✓ Seeded isolated multi-doctor appointments for dr.smith and dr.james!");
            }
        }
    }
}


