using Microsoft.Extensions.Configuration;
using Npgsql;
using TaskFlow.Application.Abstractions;

namespace TaskFlow.Infrastructure.Health;

/// <summary>
/// Sondea PostgreSQL Cloud abriendo y cerrando una conexión Npgsql.
/// `DATABASE_URL` puede venir en URI libpq o keyword/value; se traduce a
/// keyword/value porque Npgsql no interpreta URIs.
/// </summary>
public sealed class NpgsqlDatabaseHealthChecker : IDatabaseHealthChecker
{
    private const int ConnectionTimeoutSeconds = 5;

    private readonly string? _connectionString;

    public NpgsqlDatabaseHealthChecker(IConfiguration configuration)
    {
        _connectionString = ConnectionStringResolver.Normalize(
            configuration["DATABASE_URL"]
            ?? configuration["ConnectionStrings:TaskFlow"]);
    }

    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            return false;
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(ConnectionTimeoutSeconds));

        try
        {
            var builder = new NpgsqlConnectionStringBuilder(_connectionString)
            {
                Timeout = ConnectionTimeoutSeconds,
            };

            await using var connection = new NpgsqlConnection(builder.ConnectionString);
            await connection.OpenAsync(timeout.Token);
            return true;
        }
        catch (Exception exception) when (exception is NpgsqlException
            or ArgumentException
            or UriFormatException
            or TimeoutException
            or OperationCanceledException
            or InvalidOperationException
            or UnauthorizedAccessException)
        {
            return false;
        }
    }
}