using ClinicManagementSystem.Application.Interfaces;
using ClinicManagementSystem.Application.Services;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace ClinicManagementSystem.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<ILinkGeneratorService, LinkGeneratorService>();

        // AutoMapper
        services.AddAutoMapper(typeof(DependencyInjection).Assembly);

        // MediatR — registers all handlers from this assembly
        services.AddMediatR(cfg => {
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
            cfg.AddOpenBehavior(typeof(ClinicManagementSystem.Application.Behaviors.LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(ClinicManagementSystem.Application.Behaviors.ValidationBehavior<,>));
        });

        // FluentValidation — auto-scans for all AbstractValidator<T> classes
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        return services;
    }
}