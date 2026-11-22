using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaskFlow.Application.Abstractions;
using TaskFlow.Infrastructure.Health;

namespace TaskFlow.Infrastructure;

/// <summary>Registro de la capa de infraestructura (persistencia vendor-agnostic).</summary>
public static class InfrastructureDependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IDatabaseHealthChecker, NpgsqlDatabaseHealthChecker>();
        return services;
    }
}