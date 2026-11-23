using Xunit;
using TaskFlow.Application.Tasks;

namespace TaskFlow.Api.Tests.Application;

public sealed class TaskValidatorTests
{
    private static readonly CreateTaskRequestValidator CreateValidator = new();
    private static readonly UpdateTaskRequestValidator UpdateValidator = new();

    private static CreateTaskRequest ValidCreate() => new()
    {
        Title = "Escribir la documentación",
        Description = "Detalle opcional",
        Assignee = "qa-bot",
        DueDate = new DateTimeOffset(2026, 12, 1, 0, 0, 0, TimeSpan.Zero),
    };

    [Fact]
    public async Task Create_WithValidPayload_Passes()
    {
        var result = await CreateValidator.ValidateAsync(ValidCreate());

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Create_WithDefaultStatusAndPriority_Passes()
    {
        var request = ValidCreate() with { Status = null, Priority = null };

        var result = await CreateValidator.ValidateAsync(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Create_WithEmptyTitle_Fails()
    {
        var result = await CreateValidator.ValidateAsync(ValidCreate() with { Title = "" });

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateTaskRequest.Title));
    }

    [Fact]
    public async Task Create_WithShortTitle_Fails()
    {
        var result = await CreateValidator.ValidateAsync(ValidCreate() with { Title = "ab" });

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateTaskRequest.Title));
    }

    [Fact]
    public async Task Create_WithLongTitle_Fails()
    {
        var result = await CreateValidator.ValidateAsync(ValidCreate() with { Title = new string('x', 101) });

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateTaskRequest.Title));
    }

    [Fact]
    public async Task Create_WithMissingAssignee_Fails()
    {
        var result = await CreateValidator.ValidateAsync(ValidCreate() with { Assignee = "" });

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateTaskRequest.Assignee));
    }

    [Fact]
    public async Task Create_WithLongDescription_Fails()
    {
        var result = await CreateValidator.ValidateAsync(ValidCreate() with { Description = new string('d', 501) });

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateTaskRequest.Description));
    }

    [Fact]
    public async Task Create_WithMissingDueDate_Fails()
    {
        var result = await CreateValidator.ValidateAsync(ValidCreate() with { DueDate = null });

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateTaskRequest.DueDate));
    }

    [Fact]
    public async Task Create_WithUnknownStatus_Fails()
    {
        var result = await CreateValidator.ValidateAsync(ValidCreate() with { Status = "nope" });

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateTaskRequest.Status));
    }

    [Fact]
    public async Task Create_WithUnknownPriority_Fails()
    {
        var result = await CreateValidator.ValidateAsync(ValidCreate() with { Priority = "urgente" });

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateTaskRequest.Priority));
    }

    [Fact]
    public async Task Update_WithEmptyPayload_Passes()
    {
        var result = await UpdateValidator.ValidateAsync(new UpdateTaskRequest());

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Update_WithPartialValidPayload_Passes()
    {
        var result = await UpdateValidator.ValidateAsync(new UpdateTaskRequest { Title = "Nuevo título" });

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Update_WithShortTitle_Fails()
    {
        var result = await UpdateValidator.ValidateAsync(new UpdateTaskRequest { Title = "ab" });

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(UpdateTaskRequest.Title));
    }

    [Fact]
    public async Task Update_WithUnknownStatus_Fails()
    {
        var result = await UpdateValidator.ValidateAsync(new UpdateTaskRequest { Status = "nope" });

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(UpdateTaskRequest.Status));
    }

    [Fact]
    public async Task Update_WithBlankAssignee_Fails()
    {
        var result = await UpdateValidator.ValidateAsync(new UpdateTaskRequest { Assignee = "   " });

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(UpdateTaskRequest.Assignee));
    }

    [Fact]
    public async Task Update_WithLongDescription_Fails()
    {
        var result = await UpdateValidator.ValidateAsync(new UpdateTaskRequest { Description = new string('d', 501) });

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(UpdateTaskRequest.Description));
    }
}