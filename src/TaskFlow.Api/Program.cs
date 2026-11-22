using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using TaskFlow.Api.Contracts;
using TaskFlow.Api.Middleware;
using TaskFlow.Application;
using TaskFlow.Application.Abstractions;
using TaskFlow.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
{
    var rawOrigins = builder.Configuration["CORS_ORIGIN"]
        ?? builder.Configuration["Cors:Origins"]
        ?? "*";

    var origins = rawOrigins
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Where(origin => origin.Length > 0)
        .ToArray();

    if (origins.Length == 0 || origins.Contains("*", StringComparer.Ordinal))
    {
        policy.AllowAnyOrigin();
    }
    else
    {
        policy.WithOrigins(origins);
    }

    policy.AllowAnyHeader().AllowAnyMethod();
});

ConfigureRateLimiting(builder);

var app = builder.Build();

app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseCors();

if (app.Services.GetService<RateLimiterOptions>() is not null)
{
    app.UseRateLimiter();
}

app.MapGet("/health", async (
    IDatabaseHealthChecker database,
    ILoggerFactory loggerFactory,
    CancellationToken cancellationToken) =>
{
    var logger = loggerFactory.CreateLogger("TaskFlow.Health");
    var databaseUp = await database.IsAvailableAsync(cancellationToken);

    if (!databaseUp)
    {
        logger.LogWarning("Health check failed: PostgreSQL is unreachable");

        var failure = new ErrorResponse
        {
            StatusCode = StatusCodes.Status503ServiceUnavailable,
            Error = "Service Unavailable",
            Message = "Database is unavailable",
            Timestamp = DateTimeOffset.UtcNow,
            Path = "/health",
        };

        return Results.Json(failure, statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    var payload = new HealthResponse
    {
        Status = "ok",
        Service = "taskflow-api-csharp",
        Database = "up",
        Timestamp = DateTimeOffset.UtcNow,
    };

    return Results.Json(payload, statusCode: StatusCodes.Status200OK);
}).WithName("Health").WithTags("Health");

app.MapPost("/tasks", async (
    IServiceProvider services,
    CreateTaskRequest request,
    CancellationToken cancellationToken) =>
{
    var dbContext = services.GetRequiredService<TaskFlowDbContext>();

    var task = new Domain.Tasks.Task
    {
        Id = Guid.NewGuid(),
        Title = request.Title,
        Description = request.Description,
        Status = TaskStatus.Pendiente,
        Priority = TaskPriority.Media,
        Assignee = request.Assignee,
        DueDate = request.DueDate,
        CreatedAt = DateTimeOffset.UtcNow,
        UpdatedAt = DateTimeOffset.UtcNow,
    };

    dbContext.Tasks.Add(task);
    await dbContext.SaveChangesAsync(cancellationToken);

    return Results.Ok(new
    {
        id = task.Id,
        title = task.Title,
        description = task.Description,
        status = task.Status,
        priority = task.Priority,
        assignee = task.Assignee,
        dueDate = task.DueDate,
        createdAt = task.CreatedAt,
        updatedAt = task.UpdatedAt,
    });
}).WithName("CreateTask").WithTags("Tasks");

app.MapGet("/tasks", async (
    IServiceProvider services,
    string? status,
    string? priority,
    string? assignee,
    int page = 1,
    int limit = 10,
    CancellationToken cancellationToken) =>
{
    var dbContext = services.GetRequiredService<TaskFlowDbContext>();

    var query = dbContext.Tasks.AsQueryable();

    if (!string.IsNullOrWhiteSpace(status))
    {
        query = query.Where(t => t.Status.ToString() == status);
    }

    if (!string.IsNullOrWhiteSpace(priority))
    {
        query = query.Where(t => t.Priority.ToString() == priority);
    }

    if (!string.IsNullOrWhiteSpace(assignee))
    {
        query = query.Where(t => t.Assignee == assignee);
    }

    var total = await query.CountAsync(cancellationToken);
    var items = await query
        .OrderByDescending(t => t.CreatedAt)
        .Skip((page - 1) * limit)
        .Take(limit)
        .ToListAsync(cancellationToken);

    var taskDtos = items.Select(t => new
    {
        id = t.Id,
        title = t.Title,
        description = t.Description,
        status = t.Status,
        priority = t.Priority,
        assignee = t.Assignee,
        dueDate = t.DueDate,
        createdAt = t.CreatedAt,
        updatedAt = t.UpdatedAt,
    }).ToList();

    return new
    {
        data = taskDtos,
        meta = new { total, page, limit, lastPage = (int)Math.Ceiling((double)total / limit) },
    };
}).WithName("GetTasks").WithTags("Tasks");

app.MapGet("/tasks/{id:guid}", async (
    IServiceProvider services,
    Guid id,
    CancellationToken cancellationToken) =>
{
    var dbContext = services.GetRequiredService<TaskFlowDbContext>();

    var task = await dbContext.Tasks.FindAsync(id);

    if (task is null)
    {
        return Results.NotFound();
    }

    return new
    {
        id = task.Id,
        title = task.Title,
        description = task.Description,
        status = task.Status,
        priority = task.Priority,
        assignee = task.Assignee,
        dueDate = task.DueDate,
        createdAt = task.CreatedAt,
        updatedAt = task.UpdatedAt,
    };
}).WithName("GetTask").WithTags("Tasks");

app.MapPatch("/tasks/{id:guid}", async (
    IServiceProvider services,
    Guid id,
    UpdateTaskRequest request,
    CancellationToken cancellationToken) =>
{
    var dbContext = services.GetRequiredService<TaskFlowDbContext>();

    var task = await dbContext.Tasks.FindAsync(id);

    if (task is null)
    {
        return Results.NotFound();
    }

    if (!string.IsNullOrWhiteSpace(request.Title))
        task.Title = request.Title;

    if (request.Description is not null)
        task.Description = request.Description;

    if (request.Status != null)
        task.Status = request.Status switch
        {
            "pendiente" => TaskStatus.Pendiente,
            "en_curso" => TaskStatus.EnCurso,
            "hecha" => TaskStatus.Hecha,
            _ => task.Status,
        };

    if (request.Priority != null)
        task.Priority = request.Priority switch
        {
            "baja" => TaskPriority.Baja,
            "media" => TaskPriority.Media,
            "alta" => TaskPriority.Alta,
            _ => task.Priority,
        };

    if (!string.IsNullOrWhiteSpace(request.Assignee))
        task.Assignee = request.Assignee;

    if (request.DueDate != default)
        task.DueDate = request.DueDate;

    task.UpdatedAt = DateTimeOffset.UtcNow;

    await dbContext.SaveChangesAsync(cancellationToken);

    return new
    {
        id = task.Id,
        title = task.Title,
        description = task.Description,
        status = task.Status,
        priority = task.Priority,
        assignee = task.Assignee,
        dueDate = task.DueDate,
        createdAt = task.CreatedAt,
        updatedAt = task.UpdatedAt,
    };
}).WithName("UpdateTask").WithTags("Tasks");

app.MapDelete("/tasks/{id:guid}", async (
    IServiceProvider services,
    Guid id,
    CancellationToken cancellationToken) =>
{
    var dbContext = services.GetRequiredService<TaskFlowDbContext>();

    var task = await dbContext.Tasks.FindAsync(id);

    if (task is null)
    {
        return Results.NotFound();
    }

    dbContext.Tasks.Remove(task);
    await dbContext.SaveChangesAsync(cancellationToken);

    return Results.NoContent();
}).WithName("DeleteTask").WithTags("Tasks");

app.Run();

static void ConfigureRateLimiting(WebApplicationBuilder builder)
{
    var enabled = !bool.TryParse(builder.Configuration["RATE_LIMIT_ENABLED"], out var parsed) || parsed;

    if (!enabled)
    {
        return;
    }

    var permitLimit = int.TryParse(builder.Configuration["RATE_LIMIT_REQUESTS"], out var requests) && requests > 0
        ? requests
        : 120;

    var windowSeconds = int.TryParse(builder.Configuration["RATE_LIMIT_WINDOW_SECONDS"], out var window) && window > 0
        ? window
        : 60;

    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.OnRejected = (context, cancellationToken) =>
        {
            var payload = new ErrorResponse
            {
                StatusCode = StatusCodes.Status429TooManyRequests,
                Error = "Too Many Requests",
                Message = "Rate limit exceeded",
                Timestamp = DateTimeOffset.UtcNow,
                Path = context.HttpContext.Request.Path.Value ?? "/",
            };

            return new ValueTask(context.HttpContext.Response.WriteAsJsonAsync(payload, cancellationToken: cancellationToken));
        };

        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = permitLimit,
                    Window = TimeSpan.FromSeconds(windowSeconds),
                    QueueLimit = 0,
                }));
    });
}

/// <summary>Expuesto para WebApplicationFactory<Program> en los tests de integración.</summary>
public partial class Program;