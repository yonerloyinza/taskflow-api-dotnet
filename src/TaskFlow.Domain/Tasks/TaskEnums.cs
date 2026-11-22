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

/// <summary>Traducción entre los literales del contrato y las enumeraciones de dominio.</summary>
public static class TaskStatusExtensions
{
    public static string ToContractValue(this TaskStatus status) => status switch
    {
        TaskStatus.Pendiente => "pendiente",
        TaskStatus.EnCurso => "en_curso",
        TaskStatus.Hecha => "hecha",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Estado no soportado"),
    };

    public static bool TryParseContract(string? value, out TaskStatus status)
    {
        switch (value?.Trim())
        {
            case "pendiente":
                status = TaskStatus.Pendiente;
                return true;
            case "en_curso":
                status = TaskStatus.EnCurso;
                return true;
            case "hecha":
                status = TaskStatus.Hecha;
                return true;
            default:
                status = TaskStatus.Pendiente;
                return false;
        }
    }
}

/// <summary>Traducción entre los literales del contrato y las prioridades de dominio.</summary>
public static class TaskPriorityExtensions
{
    public static string ToContractValue(this TaskPriority priority) => priority switch
    {
        TaskPriority.Baja => "baja",
        TaskPriority.Media => "media",
        TaskPriority.Alta => "alta",
        _ => throw new ArgumentOutOfRangeException(nameof(priority), priority, "Prioridad no soportada"),
    };

    public static bool TryParseContract(string? value, out TaskPriority priority)
    {
        switch (value?.Trim())
        {
            case "baja":
                priority = TaskPriority.Baja;
                return true;
            case "media":
                priority = TaskPriority.Media;
                return true;
            case "alta":
                priority = TaskPriority.Alta;
                return true;
            default:
                priority = TaskPriority.Media;
                return false;
        }
    }
}