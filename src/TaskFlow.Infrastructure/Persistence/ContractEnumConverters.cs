using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TaskFlow.Domain.Tasks;
using TaskPriority = TaskFlow.Domain.Tasks.TaskPriority;
using TaskStatus = TaskFlow.Domain.Tasks.TaskStatus;

namespace TaskFlow.Infrastructure.Persistence;

/// <summary>Conversores EF Core de enumeraciones a los valores literales del contrato OpenAPI.</summary>
internal static class ContractEnumConverters
{
    public static readonly ValueConverter<TaskStatus, string> Status = new(
        status => status.ToContractValue(),
        value => ParseStatus(value));

    public static readonly ValueConverter<TaskPriority, string> Priority = new(
        priority => priority.ToContractValue(),
        value => ParsePriority(value));

    private static TaskStatus ParseStatus(string value)
        => TaskStatusExtensions.TryParseContract(value, out var status)
            ? status
            : throw new ArgumentOutOfRangeException(nameof(value), value, "Estado no soportado");

    private static TaskPriority ParsePriority(string value)
        => TaskPriorityExtensions.TryParseContract(value, out var priority)
            ? priority
            : throw new ArgumentOutOfRangeException(nameof(value), value, "Prioridad no soportada");
}