using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaskFlow.Application.Abstractions;
using TaskFlow.Infrastructure.Health;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.Infrastructure;

/// <summary>Registro de la capa de infraestructura (persistencia vendor-agnostic).</summary>
public static class InfrastructureDependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = ConnectionStringResolver.Normalize(
            configuration["DATABASE_URL"]
            ?? configuration["ConnectionStrings:TaskFlow"])
            ?? "Host=127.0.0.1;Port=5432;Database=taskflow;Username=postgres;Password=postgres";

        services.AddSingleton<IDatabaseHealthChecker, NpgsqlDatabaseHealthChecker>();

        services.AddDbContext<TaskFlowDbContext>(options => options
            .UseNpgsql(connectionString));

        return services;
    }
}