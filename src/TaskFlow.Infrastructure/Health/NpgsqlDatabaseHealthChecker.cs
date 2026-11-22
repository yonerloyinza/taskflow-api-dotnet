using Microsoft.Extensions.Configuration;
using TaskFlow.Application.Abstractions;

namespace TaskFlow.Infrastructure.Health;

/// <summary>
/// Abre una conexión Npgsql contra `DATABASE_URL` y la cierra de inmediato.
/// No usa EF Core todavía: sirve para `GET /health` sin arrastrar el modelo.
/// </summary>
public sealed class NpgsqlDatabaseHealthChecker : IDatabaseHealthChecker
{
    private readonly string? _connectionString;

    public NpgsqlDatabaseHealthChecker(IConfiguration configuration)
    {
        _connectionString = Normalize(
            configuration["DATABASE_URL"]
            ?? configuration["ConnectionStrings:TaskFlow"]);
    }

    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            return false;
        }

        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            return true;
        }
        catch (Exception exception) when (exception is NpgsqlException or TimeoutException or InvalidOperationException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim().Trim('"', '\'');
    }
}