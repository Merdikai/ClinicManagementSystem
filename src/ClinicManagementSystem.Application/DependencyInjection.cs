using ClinicManagementSystem.Application.Interfaces;
using ClinicManagementSystem.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ClinicManagementSystem.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // AutoMapper
        services.AddAutoMapper(typeof(DependencyInjection).Assembly);

        // Services
        services.AddScoped<IPatientService, PatientService>();
        services.AddScoped<IAppointmentService, AppointmentService>();
        services.AddScoped<IVitalSignService, VitalSignService>();
        services.AddScoped<IConsultationService, ConsultationService>();
        services.AddScoped<IMedicineService, MedicineService>();
        services.AddScoped<IBillingService, BillingService>();

        return services;
    }
}