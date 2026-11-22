using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace TaskFlow.Application;

/// <summary>Registro de la capa de aplicación (validadores FluentValidation).</summary>
public static class ApplicationDependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(ApplicationDependencyInjection).Assembly);
        return services;
    }
}