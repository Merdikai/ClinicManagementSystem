using ClinicManagementSystem.Application.Interfaces;
using ClinicManagementSystem.Domain.Interfaces;
using ClinicManagementSystem.Infrastructure.Identity;
using ClinicManagementSystem.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace ClinicManagementSystem.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        // Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IPatientRepository, PatientRepository>();
        services.AddScoped<IAppointmentRepository, AppointmentRepository>();
        services.AddScoped<IDoctorScheduleRepository, DoctorScheduleRepository>();
        services.AddScoped<IVitalSignRepository, VitalSignRepository>();
        services.AddScoped<IConsultationRepository, ConsultationRepository>();
        services.AddScoped<IPrescriptionRepository, PrescriptionRepository>();
        services.AddScoped<IMedicineRepository, MedicineRepository>();
        services.AddScoped<IInvoiceRepository, InvoiceRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();

        // Identity Services (register as their interfaces)
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();

        services.AddScoped<IAuditService, ClinicManagementSystem.Infrastructure.Services.AuditService>();

        return services;
    }
}