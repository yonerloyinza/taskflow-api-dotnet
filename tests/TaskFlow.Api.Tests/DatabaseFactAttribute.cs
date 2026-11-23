using Xunit;

namespace TaskFlow.Api.Tests;

/// <summary>
/// Test que requiere PostgreSQL. Se omite cuando no hay `DATABASE_URL`
/// (por ejemplo, al ejecutar la suite sin la base de datos del CI).
/// </summary>
public sealed class DatabaseFact : FactAttribute
{
    public DatabaseFact()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DATABASE_URL")))
        {
            Skip = "DATABASE_URL no está definida";
        }
    }
}