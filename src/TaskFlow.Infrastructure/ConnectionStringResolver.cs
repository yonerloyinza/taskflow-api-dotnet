using Npgsql;

namespace TaskFlow.Infrastructure;

/// <summary>
/// Traduce la URI libpq (`postgresql://user:pass@host:5432/db?sslmode=require`) al
/// formato keyword/value que interpreta Npgsql. Devuelve `null` si no hay cadena.
/// </summary>
public static class ConnectionStringResolver
{
    private const string PostgresUriScheme = "postgres://";
    private const string PostgresqlUriScheme = "postgresql://";
    private const int DefaultPort = 5432;

    public static string? Normalize(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var trimmed = raw.Trim().Trim('"', '\'');

        return IsUri(trimmed) ? ToKeywordString(trimmed) : trimmed;
    }

    private static bool IsUri(string value)
        => value.StartsWith(PostgresUriScheme, StringComparison.OrdinalIgnoreCase)
        || value.StartsWith(PostgresqlUriScheme, StringComparison.OrdinalIgnoreCase);

    private static string ToKeywordString(string uriString)
    {
        var uri = new Uri(uriString);
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : DefaultPort,
            Database = uri.AbsolutePath.Trim('/'),
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
            var value = equals >= 0 ? parameter[(equals + 1)..] : string.Empty;

            if (key.Length > 0)
            {
                builder[Uri.UnescapeDataString(key)] = Uri.UnescapeDataString(value);
            }
        }

        return builder.ConnectionString;
    }
}