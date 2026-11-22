using Microsoft.Extensions.Configuration;
using Npgsql;
using TaskFlow.Application.Abstractions;

namespace TaskFlow.Infrastructure.Health;

/// <summary>
/// Sondea PostgreSQL Cloud abriendo y cerrando una conexión Npgsql.
/// `DATABASE_URL` puede venir en formato URI (libpq) o keyword/value; se traduce
/// a keyword/value porque Npgsql no interpreta URIs.
/// </summary>
public sealed class NpgsqlDatabaseHealthChecker : IDatabaseHealthChecker
{
    private const string PostgresUriScheme = "postgres://";
    private const string PostgresqlUriScheme = "postgresql://";
    private const int DefaultPort = 5432;
    private const int ConnectionTimeoutSeconds = 5;

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

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(ConnectionTimeoutSeconds));

        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
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

    private static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim().Trim('"', '\'');

        return IsUri(trimmed) ? ToKeywordString(trimmed) : trimmed;
    }

    private static bool IsUri(string value)
        => value.StartsWith(PostgresUriScheme, StringComparison.OrdinalIgnoreCase)
        || value.StartsWith(PostgresqlUriScheme, StringComparison.OrdinalIgnoreCase);

    /// <summary>Convierte `postgresql://user:pass@host:port/db?sslmode=require` a keyword/value.</summary>
    private static string ToKeywordString(string uriString)
    {
        var uri = new Uri(uriString);
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : DefaultPort,
            Database = uri.AbsolutePath.Trim('/'),
            Timeout = ConnectionTimeoutSeconds,
        };

        var credentials = uri.UserInfo;
        var separator = credentials.IndexOf(':');

        if (separator >= 0)
        {
            builder.Username = Uri.UnescapeDataString(credentials[..separator]);
            builder.Password = Uri.UnescapeDataString(credentials[(separator + 1)..]);
        }
        else if (credentials.Length > 0)
        {
            builder.Username = Uri.UnescapeDataString(credentials);
        }

        foreach (var parameter in uri.Query
            .TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var equals = parameter.IndexOf('=');
            var key = equals >= 0 ? parameter[..equals] : parameter;
            var val = equals >= 0 ? parameter[(equals + 1)..] : string.Empty;

            if (key.Length > 0)
            {
                builder[Uri.UnescapeDataString(key)] = Uri.UnescapeDataString(val);
            }
        }

        return builder.ConnectionString;
    }
}