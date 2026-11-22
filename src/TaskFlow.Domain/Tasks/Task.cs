namespace TaskFlow.Domain.Tasks;

/// <summary>Tarea canónica del contrato OpenAPI 3.1 (`Task`).</summary>
public sealed class Task
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public TaskStatus Status { get; set; } = TaskStatus.Pendiente;

    public TaskPriority Priority { get; set; } = TaskPriority.Media;

    public string Assignee { get; set; } = string.Empty;

    public DateTimeOffset DueDate { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}