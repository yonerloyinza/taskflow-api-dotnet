using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore.Design;

namespace TaskFlow.Infrastructure.Persistence;

/// <summary>
/// Fábrica para las herramientas EF Core (`dotnet ef`). Usa `DIRECT_URL` (5432)
/// para migraciones y cae a `DATABASE_URL` si no está definida.
/// </summary>
public sealed class TaskFlowDbContextFactory : IDesignTimeDbContextFactory<TaskFlowDbContext>
{
    public TaskFlowDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build();

        var connectionString = ConnectionStringResolver.Normalize(
            configuration["DIRECT_URL"]
            ?? configuration["DATABASE_URL"]
            ?? configuration["ConnectionStrings:TaskFlow"]);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "DIRECT_URL (o DATABASE_URL) es obligatoria para las migraciones de EF Core.");
        }

        var options = new DbContextOptionsBuilder<TaskFlowDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new TaskFlowDbContext(options);
    }
}