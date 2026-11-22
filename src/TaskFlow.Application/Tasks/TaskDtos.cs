namespace TaskFlow.Application.Tasks;

/// <summary>Cuerpo de `POST /tasks` (contrato `CreateTaskRequest`).</summary>
public sealed class CreateTaskRequest
{
    public string Title { get; init; } = string.Empty;

    public string? Description { get; init; }

    public string? Status { get; init; }

    public string? Priority { get; init; }

    public string Assignee { get; init; } = string.Empty;

    public DateTimeOffset? DueDate { get; init; }
}

/// <summary>Cuerpo de `PATCH /tasks/{id}` (contrato `UpdateTaskRequest`).</summary>
public sealed class UpdateTaskRequest
{
    public string? Title { get; init; }

    public string? Description { get; init; }

    public string? Status { get; init; }

    public string? Priority { get; init; }

    public string? Assignee { get; init; }

    public DateTimeOffset? DueDate { get; init; }
}

/// <summary>Salida canónica de una tarea (contrato `Task`).</summary>
public sealed record TaskResponse(
    Guid Id,
    string Title,
    string? Description,
    string Status,
    string Priority,
    string Assignee,
    DateTimeOffset DueDate,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>Bloque `meta` de `PaginatedTasksResponse`.</summary>
public sealed record PaginationMeta(int Total, int Page, int Limit, int LastPage);

/// <summary>Salida de `GET /tasks` (contrato `PaginatedTasksResponse`).</summary>
public sealed record PaginatedTasksResponse(IReadOnlyList<TaskResponse> Data, PaginationMeta Meta);