using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace TaskFlow.Infrastructure.Persistence;

/// <summary>Conversores EF Core de enumeraciones a los valores literales del contrato OpenAPI.</summary>
internal static class ContractEnumConverters
{
    public static readonly ValueConverter<global::TaskFlow.Domain.Tasks.TaskStatus, string> TaskStatus = new(
        status => status switch
        {
            TaskFlow.Domain.Tasks.TaskStatus.Pendiente => "pendiente",
            TaskFlow.Domain.Tasks.TaskStatus.EnCurso => "en_curso",
            TaskFlow.Domain.Tasks.TaskStatus.Hecha => "hecha",
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Estado no soportado"),
        },
        value => value switch
        {
            "pendiente" => TaskFlow.Domain.Tasks.TaskStatus.Pendiente,
            "en_curso" => TaskFlow.Domain.Tasks.TaskStatus.EnCurso,
            "hecha" => TaskFlow.Domain.Tasks.TaskStatus.Hecha,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Estado no soportado"),
        });

    public static readonly ValueConverter<global::TaskFlow.Domain.Tasks.TaskPriority, string> TaskPriority = new(
        priority => priority switch
        {
            TaskFlow.Domain.Tasks.TaskPriority.Baja => "baja",
            TaskFlow.Domain.Tasks.TaskPriority.Media => "media",
            TaskFlow.Domain.Tasks.TaskPriority.Alta => "alta",
            _ => throw new ArgumentOutOfRangeException(nameof(priority), priority, "Prioridad no soportada"),
        },
        value => value switch
        {
            "baja" => TaskFlow.Domain.Tasks.TaskPriority.Baja,
            "media" => TaskFlow.Domain.Tasks.TaskPriority.Media,
            "alta" => TaskFlow.Domain.Tasks.TaskPriority.Alta,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Prioridad no soportada"),
        });
}