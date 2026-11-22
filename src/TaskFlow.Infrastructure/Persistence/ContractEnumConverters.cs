using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TaskFlow.Domain.Tasks;

namespace TaskFlow.Infrastructure.Persistence;

/// <summary>Conversores EF Core de enumeraciones a los valores literales del contrato OpenAPI.</summary>
internal static class ContractEnumConverters
{
    public static readonly ValueConverter<TaskStatus, string> TaskStatus = new(
        status => status switch
        {
            Domain.Tasks.TaskStatus.Pendiente => "pendiente",
            Domain.Tasks.TaskStatus.EnCurso => "en_curso",
            Domain.Tasks.TaskStatus.Hecha => "hecha",
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Estado no soportado"),
        },
        value => value switch
        {
            "pendiente" => Domain.Tasks.TaskStatus.Pendiente,
            "en_curso" => Domain.Tasks.TaskStatus.EnCurso,
            "hecha" => Domain.Tasks.TaskStatus.Hecha,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Estado no soportado"),
        });

    public static readonly ValueConverter<TaskPriority, string> TaskPriority = new(
        priority => priority switch
        {
            Domain.Tasks.TaskPriority.Baja => "baja",
            Domain.Tasks.TaskPriority.Media => "media",
            Domain.Tasks.TaskPriority.Alta => "alta",
            _ => throw new ArgumentOutOfRangeException(nameof(priority), priority, "Prioridad no soportada"),
        },
        value => value switch
        {
            "baja" => Domain.Tasks.TaskPriority.Baja,
            "media" => Domain.Tasks.TaskPriority.Media,
            "alta" => Domain.Tasks.TaskPriority.Alta,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Prioridad no soportada"),
        });
}