namespace TaskFlow.Api.Contracts;

/// <summary>
/// Cuerpo de error canónico del contrato OpenAPI 3.1 (`ErrorResponse`).
/// `Message` serializa como string o como array de strings.
/// </summary>
public sealed class ErrorResponse
{
    public int StatusCode { get; init; }

    public string Error { get; init; } = "Error";

    public object Message { get; init; } = string.Empty;

    public DateTimeOffset Timestamp { get; init; }

    public string Path { get; init; } = "/";
}