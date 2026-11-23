using Xunit;
using TaskFlow.Application.Tasks;
using TaskEntity = TaskFlow.Domain.Tasks.Task;
using TaskPriority = TaskFlow.Domain.Tasks.TaskPriority;
using TaskStatus = TaskFlow.Domain.Tasks.TaskStatus;

namespace TaskFlow.Api.Tests.Application;

public sealed class TaskMappersTests
{
    private static TaskEntity Sample() => new()
    {
        Id = Guid.NewGuid(),
        Title = "Tarea de ejemplo",
        Description = "Detalle",
        Status = TaskStatus.EnCurso,
        Priority = TaskPriority.Alta,
        Assignee = "qa-bot",
        DueDate = new DateTimeOffset(2026, 12, 1, 0, 0, 0, TimeSpan.Zero),
        CreatedAt = new DateTimeOffset(2026, 11, 1, 10, 0, 0, TimeSpan.Zero),
        UpdatedAt = new DateTimeOffset(2026, 11, 2, 10, 0, 0, TimeSpan.Zero),
    };

    [Fact]
    public void ToResponse_MapsContractValues()
    {
        var response = Sample().ToResponse();

        Assert.Equal("en_curso", response.Status);
        Assert.Equal("alta", response.Priority);
        Assert.Equal("qa-bot", response.Assignee);
        Assert.Equal("Tarea de ejemplo", response.Title);
    }

    [Fact]
    public void ApplyUpdate_ChangesOnlyProvidedFields()
    {
        var task = Sample();

        task.ApplyUpdate(new UpdateTaskRequest { Title = "Otro título" });

        Assert.Equal("Otro título", task.Title);
        Assert.Equal(TaskStatus.EnCurso, task.Status);
        Assert.Equal(TaskPriority.Alta, task.Priority);
        Assert.Equal("qa-bot", task.Assignee);
    }

    [Fact]
    public void ApplyUpdate_TrimsStringsAndClearsDescription()
    {
        var task = Sample();

        task.ApplyUpdate(new UpdateTaskRequest { Title = "  recortado  ", Assignee = "  dev  ", Description = "   " });

        Assert.Equal("recortado", task.Title);
        Assert.Equal("dev", task.Assignee);
        Assert.Null(task.Description);
    }

    [Fact]
    public void ApplyUpdate_ChangesStatusPriorityAndDueDate()
    {
        var task = Sample();
        var due = new DateTimeOffset(2027, 1, 15, 0, 0, 0, TimeSpan.Zero);

        task.ApplyUpdate(new UpdateTaskRequest { Status = "hecha", Priority = "baja", DueDate = due });

        Assert.Equal(TaskStatus.Hecha, task.Status);
        Assert.Equal(TaskPriority.Baja, task.Priority);
        Assert.Equal(due, task.DueDate);
    }
}