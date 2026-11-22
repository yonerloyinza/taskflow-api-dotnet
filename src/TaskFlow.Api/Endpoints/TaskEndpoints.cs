using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Api.Contracts;
using TaskFlow.Application.Tasks;
using TaskFlow.Domain.Tasks;
using TaskFlow.Infrastructure.Persistence;
using TaskEntity = TaskFlow.Domain.Tasks.Task;
using TaskPriority = TaskFlow.Domain.Tasks.TaskPriority;
using TaskStatus = TaskFlow.Domain.Tasks.TaskStatus;

namespace TaskFlow.Api.Endpoints;

/// <summary>
/// Endpoints CRUD de `docs/openapi.yaml`: rutas, filtros (`status`, `priority`,
/// `assignee`), paginación y errores `ErrorResponse`.
/// </summary>
public static class TaskEndpoints
{
    public static IEndpointRouteBuilder MapTaskEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var tasks = endpoints.MapGroup("/tasks").WithTags("Tasks");

        tasks.MapPost("", CreateAsync)
            .WithName("CreateTask")
            .WithSummary("Crear una nueva tarea")
            .AddEndpointFilter<ValidationFilter<CreateTaskRequest>>()
            .Produces<TaskResponse>(StatusCodes.Status201Created)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest);

        tasks.MapGet("", ListAsync)
            .WithName("GetTasks")
            .WithSummary("Listar tareas con filtros y paginación")
            .Produces<PaginatedTasksResponse>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest);

        tasks.MapGet("/{id:guid}", GetAsync)
            .WithName("GetTask")
            .WithSummary("Obtener detalle de una tarea")
            .Produces<TaskResponse>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        tasks.MapPatch("/{id:guid}", UpdateAsync)
            .WithName("UpdateTask")
            .WithSummary("Actualizar parcialmente una tarea")
            .AddEndpointFilter<ValidationFilter<UpdateTaskRequest>>()
            .Produces<TaskResponse>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        tasks.MapDelete("/{id:guid}", DeleteAsync)
            .WithName("DeleteTask")
            .WithSummary("Eliminar una tarea")
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        return endpoints;
    }

    private static async Task<IResult> CreateAsync(
        CreateTaskRequest request,
        TaskFlowDbContext database,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        TaskStatus status = TaskStatus.Pendiente;
        TaskPriority priority = TaskPriority.Media;

        if (!string.IsNullOrWhiteSpace(request.Status)
            && !TaskStatusExtensions.TryParseContract(request.Status, out status))
        {
            return Error(httpContext, StatusCodes.Status400BadRequest, "status must be one of: pendiente, en_curso, hecha");
        }

        if (!string.IsNullOrWhiteSpace(request.Priority)
            && !TaskPriorityExtensions.TryParseContract(request.Priority, out priority))
        {
            return Error(httpContext, StatusCodes.Status400BadRequest, "priority must be one of: baja, media, alta");
        }

        var now = DateTimeOffset.UtcNow;

        var task = new TaskEntity
        {
            Id = Guid.NewGuid(),
            Title = request.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            Status = status,
            Priority = priority,
            Assignee = request.Assignee.Trim(),
            DueDate = request.DueDate!.Value,
            CreatedAt = now,
            UpdatedAt = now,
        };

        database.Tasks.Add(task);
        await database.SaveChangesAsync(cancellationToken);

        return Results.Json(task.ToResponse(), statusCode: StatusCodes.Status201Created);
    }

    private static async Task<IResult> ListAsync(
        TaskFlowDbContext database,
        HttpContext httpContext,
        string? status = null,
        string? priority = null,
        string? assignee = null,
        int page = 1,
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        limit = limit is < 1 or > 100 ? 10 : limit;

        var query = database.Tasks.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!TaskStatusExtensions.TryParseContract(status, out var parsedStatus))
            {
                return Error(httpContext, StatusCodes.Status400BadRequest, "status must be one of: pendiente, en_curso, hecha");
            }

            query = query.Where(task => task.Status == parsedStatus);
        }

        if (!string.IsNullOrWhiteSpace(priority))
        {
            if (!TaskPriorityExtensions.TryParseContract(priority, out var parsedPriority))
            {
                return Error(httpContext, StatusCodes.Status400BadRequest, "priority must be one of: baja, media, alta");
            }

            query = query.Where(task => task.Priority == parsedPriority);
        }

        if (!string.IsNullOrWhiteSpace(assignee))
        {
            var assigneeValue = assignee.Trim();
            query = query.Where(task => task.Assignee == assigneeValue);
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(task => task.CreatedAt)
            .ThenBy(task => task.Id)
            .Skip((page - 1) * limit)
            .Take(limit)
            .ToListAsync(cancellationToken);

        var data = items.Select(task => task.ToResponse()).ToArray();
        var lastPage = total == 0 ? 0 : (int)Math.Ceiling(total / (double)limit);

        return Results.Ok(new PaginatedTasksResponse(data, new PaginationMeta(total, page, limit, lastPage)));
    }

    private static async Task<IResult> GetAsync(
        TaskFlowDbContext database,
        Guid id,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var task = await database.Tasks
            .AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);

        return task is null
            ? Error(httpContext, StatusCodes.Status404NotFound, "Task not found")
            : Results.Ok(task.ToResponse());
    }

    private static async Task<IResult> UpdateAsync(
        TaskFlowDbContext database,
        Guid id,
        UpdateTaskRequest request,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.Status)
            && !TaskStatusExtensions.TryParseContract(request.Status, out _))
        {
            return Error(httpContext, StatusCodes.Status400BadRequest, "status must be one of: pendiente, en_curso, hecha");
        }

        if (!string.IsNullOrWhiteSpace(request.Priority)
            && !TaskPriorityExtensions.TryParseContract(request.Priority, out _))
        {
            return Error(httpContext, StatusCodes.Status400BadRequest, "priority must be one of: baja, media, alta");
        }

        var task = await database.Tasks.FirstOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);

        if (task is null)
        {
            return Error(httpContext, StatusCodes.Status404NotFound, "Task not found");
        }

        task.ApplyUpdate(request);
        task.UpdatedAt = DateTimeOffset.UtcNow;

        await database.SaveChangesAsync(cancellationToken);

        return Results.Ok(task.ToResponse());
    }

    private static async Task<IResult> DeleteAsync(
        TaskFlowDbContext database,
        Guid id,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var task = await database.Tasks.FirstOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);

        if (task is null)
        {
            return Error(httpContext, StatusCodes.Status404NotFound, "Task not found");
        }

        database.Tasks.Remove(task);
        await database.SaveChangesAsync(cancellationToken);

        return Results.NoContent();
    }

    private static IResult Error(HttpContext httpContext, int statusCode, string message)
        => Results.Json(
            new ErrorResponse
            {
                StatusCode = statusCode,
                Error = statusCode switch
                {
                    StatusCodes.Status400BadRequest => "Bad Request",
                    StatusCodes.Status404NotFound => "Not Found",
                    StatusCodes.Status500InternalServerError => "Internal Server Error",
                    _ => "Error",
                },
                Message = message,
                Timestamp = DateTimeOffset.UtcNow,
                Path = httpContext.Request.Path.Value ?? "/",
            },
            statusCode: statusCode);
}

/// <summary>Endpoint filter que aplica FluentValidation y responde `ErrorResponse` en 400.</summary>
public sealed class ValidationFilter<TRequest> : IEndpointFilter
    where TRequest : class
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var validator = context.HttpContext.RequestServices.GetService<IValidator<TRequest>>();
        var request = context.Arguments.OfType<TRequest>().FirstOrDefault();

        if (validator is not null && request is not null)
        {
            var result = await validator.ValidateAsync(request, context.HttpContext.RequestAborted);

            if (!result.IsValid)
            {
                var messages = result.Errors.Select(failure => failure.ErrorMessage).ToArray();

                return Results.Json(
                    new ErrorResponse
                    {
                        StatusCode = StatusCodes.Status400BadRequest,
                        Error = "Bad Request",
                        Message = messages,
                        Timestamp = DateTimeOffset.UtcNow,
                        Path = context.HttpContext.Request.Path.Value ?? "/",
                    },
                    statusCode: StatusCodes.Status400BadRequest);
            }
        }

        return await next(context);
    }
}