namespace TaskFlow.Domain.Tasks;

/// <summary>Estados canónicos del contrato OpenAPI (`TaskStatus`).</summary>
public enum TaskStatus
{
    Pendiente = 0,
    EnCurso = 1,
    Hecha = 2,
}

/// <summary>Prioridades canónicas del contrato OpenAPI (`TaskPriority`).</summary>
public enum TaskPriority
{
    Baja = 0,
    Media = 1,
    Alta = 2,
}