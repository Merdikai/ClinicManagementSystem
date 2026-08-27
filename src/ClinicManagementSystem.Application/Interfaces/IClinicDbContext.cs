using ClinicManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Application.Interfaces;

public interface IClinicDbContext
{
    DbSet<User> Users { get; }
    DbSet<Role> Roles { get; }
    DbSet<UserRole> UserRoles { get; }
    DbSet<Patient> Patients { get; }
    DbSet<MedicalRecord> MedicalRecords { get; }
    DbSet<DoctorSchedule> DoctorSchedules { get; }
    DbSet<Appointment> Appointments { get; }
    DbSet<VitalSign> VitalSigns { get; }
    DbSet<Consultation> Consultations { get; }
    DbSet<Prescription> Prescriptions { get; }
    DbSet<PrescriptionItem> PrescriptionItems { get; }
    DbSet<Medicine> Medicines { get; }
    DbSet<Invoice> Invoices { get; }
    DbSet<InvoiceItem> InvoiceItems { get; }
    DbSet<Payment> Payments { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<PasswordResetToken> PasswordResetTokens { get; }
    DbSet<LabTestTemplate> LabTestTemplates { get; }
    DbSet<LabOrder> LabOrders { get; }
    DbSet<LabOrderItem> LabOrderItems { get; }
    DbSet<LabResult> LabResults { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
