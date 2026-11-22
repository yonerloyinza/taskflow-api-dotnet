using TaskFlow.Domain.Tasks;
using TaskEntity = TaskFlow.Domain.Tasks.Task;
using TaskPriority = TaskFlow.Domain.Tasks.TaskPriority;
using TaskStatus = TaskFlow.Domain.Tasks.TaskStatus;

namespace TaskFlow.Application.Tasks;

/// <summary>Traducción entre la entidad de dominio y los DTOs del contrato.</summary>
public static class TaskMappers
{
    public static TaskResponse ToResponse(this TaskEntity task) => new(
        task.Id,
        task.Title,
        task.Description,
        task.Status.ToContractValue(),
        task.Priority.ToContractValue(),
        task.Assignee,
        task.DueDate,
        task.CreatedAt,
        task.UpdatedAt);

    /// <summary>Aplica los campos presentes en un `UpdateTaskRequest` sobre la entidad.</summary>
    public static void ApplyUpdate(this TaskEntity task, UpdateTaskRequest request)
    {
        if (request.Title is not null)
        {
            task.Title = request.Title.Trim();
        }

        if (request.Description is not null)
        {
            task.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        }

        if (request.Assignee is not null)
        {
            task.Assignee = request.Assignee.Trim();
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            task.Status = ParseStatus(request.Status);
        }

        if (!string.IsNullOrWhiteSpace(request.Priority))
        {
            task.Priority = ParsePriority(request.Priority);
        }

        if (request.DueDate is not null)
        {
            task.DueDate = request.DueDate.Value;
        }
    }

    private static TaskStatus ParseStatus(string value)
        => TaskStatusExtensions.TryParseContract(value, out var status)
            ? status
            : throw new ArgumentOutOfRangeException(nameof(value), value, "status must be one of: pendiente, en_curso, hecha");

    private static TaskPriority ParsePriority(string value)
        => TaskPriorityExtensions.TryParseContract(value, out var priority)
            ? priority
            : throw new ArgumentOutOfRangeException(nameof(value), value, "priority must be one of: baja, media, alta");
}