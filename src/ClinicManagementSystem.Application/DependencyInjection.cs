using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace ClinicManagementSystem.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // AutoMapper
        services.AddAutoMapper(typeof(DependencyInjection).Assembly);

        // MediatR — registers all handlers from this assembly
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        // FluentValidation — auto-scans for all AbstractValidator<T> classes
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        return services;
    }
}