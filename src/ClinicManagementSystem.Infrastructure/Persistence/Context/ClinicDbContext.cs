using ClinicManagementSystem.Application.Interfaces;
using ClinicManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace ClinicManagementSystem.Infrastructure.Persistence.Context;

public class ClinicDbContext : DbContext, IClinicDbContext
{
    private readonly IHttpContextAccessor? _httpContextAccessor;

    public ClinicDbContext(DbContextOptions<ClinicDbContext> options, IHttpContextAccessor? httpContextAccessor = null) : base(options)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<MedicalRecord> MedicalRecords => Set<MedicalRecord>();
    public DbSet<DoctorSchedule> DoctorSchedules => Set<DoctorSchedule>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<VitalSign> VitalSigns => Set<VitalSign>();
    public DbSet<Consultation> Consultations => Set<Consultation>();
    public DbSet<Prescription> Prescriptions => Set<Prescription>();
    public DbSet<PrescriptionItem> PrescriptionItems => Set<PrescriptionItem>();
    public DbSet<Medicine> Medicines => Set<Medicine>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
        public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<LabTestTemplate> LabTestTemplates => Set<LabTestTemplate>();
    public DbSet<LabOrder> LabOrders => Set<LabOrder>();
    public DbSet<LabOrderItem> LabOrderItems => Set<LabOrderItem>();
    public DbSet<LabResult> LabResults => Set<LabResult>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<EmrAccessAuditLog> EmrAccessAuditLogs => Set<EmrAccessAuditLog>();
    public DbSet<StaffDutyShift> StaffDutyShifts => Set<StaffDutyShift>();
    public DbSet<DoctorDelegation> DoctorDelegations => Set<DoctorDelegation>();
    public DbSet<NursingProcedure> NursingProcedures => Set<NursingProcedure>();
    public DbSet<ShiftHandover> ShiftHandovers => Set<ShiftHandover>();
    public DbSet<DailyCashReconciliation> DailyCashReconciliations => Set<DailyCashReconciliation>();
    public DbSet<VisitorInquiry> VisitorInquiries => Set<VisitorInquiry>();
    public DbSet<PharmacyStockRequisition> PharmacyStockRequisitions => Set<PharmacyStockRequisition>();
    public DbSet<ControlledSubstanceLog> ControlledSubstanceLogs => Set<ControlledSubstanceLog>();
    public DbSet<InsuranceClaim> InsuranceClaims => Set<InsuranceClaim>();
    public DbSet<BillingAdjustment> BillingAdjustments => Set<BillingAdjustment>();
    public DbSet<ExpenseTransaction> ExpenseTransactions => Set<ExpenseTransaction>();
    public DbSet<PettyCashVoucher> PettyCashVouchers => Set<PettyCashVoucher>();
    public DbSet<PayrollRecord> PayrollRecords => Set<PayrollRecord>();
    public DbSet<LabReagent> LabReagents => Set<LabReagent>();
    public DbSet<LabStockRequisition> LabStockRequisitions => Set<LabStockRequisition>();
    public DbSet<LabEquipmentLog> LabEquipmentLogs => Set<LabEquipmentLog>();
    public DbSet<RadiologyOrder> RadiologyOrders => Set<RadiologyOrder>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ClinicDbContext).Assembly);

        foreach (var property in modelBuilder.Model.GetEntityTypes()
            .SelectMany(t => t.GetProperties())
            .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
        {
            property.SetColumnType("decimal(18,2)");
        }

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (entityType.ClrType.Namespace?.Contains("Domain.Entities") == true)
            {
                modelBuilder.Entity(entityType.ClrType)
                    .Property<DateTime>("LastUpdated")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                modelBuilder.Entity(entityType.ClrType)
                    .Property<string>("LastUpdatedBy")
                    .HasMaxLength(100);
            }
        }
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Modified || e.State == EntityState.Added))
        {
            if (entry.Entity.GetType().Namespace?.Contains("Domain.Entities") == true)
            {
                entry.Property("LastUpdated").CurrentValue = DateTime.UtcNow;
                entry.Property("LastUpdatedBy").CurrentValue = _httpContextAccessor?.HttpContext?.User?.Identity?.Name ?? "system";
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}

