namespace TaskFlow.Api.Contracts;

/// <summary>Respuesta de `GET /health` (contrato OpenAPI 3.1).</summary>
public sealed class HealthResponse
{
    public string Status { get; init; } = "ok";

    public string Service { get; init; } = "taskflow-api-dotnet";

    public string Database { get; init; } = "up";

    public DateTimeOffset Timestamp { get; init; }
}