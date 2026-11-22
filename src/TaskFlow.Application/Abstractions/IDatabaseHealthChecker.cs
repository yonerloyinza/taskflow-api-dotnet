namespace TaskFlow.Application.Abstractions;

/// <summary>
/// Sondeo de disponibilidad de PostgreSQL Cloud.
/// La implementación vive en Infrastructure (Npgsql puro, sin SDKs propietarios).
/// </summary>
public interface IDatabaseHealthChecker
{
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
}