using System.Net;
using Microsoft.AspNetCore.Http;
using System.Net.Http.Json;
using Xunit;
using TaskFlow.Api.Contracts;
using TaskFlow.Application.Tasks;

namespace TaskFlow.Api.Tests.Tasks;

/// <summary>
/// Pruebas de integración contra PostgreSQL real (`DATABASE_URL`): recorren la
/// API completa y limpian los datos que crean.
/// </summary>
public sealed class TaskEndpointsTests : IAsyncLifetime
{
    private readonly TaskFlowApiFactory _factory = new();
    private readonly List<Guid> _created = new();
    private HttpClient _client = null!;

    public Task InitializeAsync()
    {
        _client = _factory.CreateClient();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        foreach (var id in _created)
        {
            await _client.DeleteAsync($"/tasks/{id}");
        }

        _client.Dispose();
        _factory.Dispose();
    }

    [DatabaseFact]
    public async Task Create_Returns201WithContractPayload()
    {
        var title = $"crear-{Guid.NewGuid():N}";

        var response = await _client.PostAsJsonAsync("/tasks", new CreateTaskRequest
        {
            Title = title,
            Assignee = "qa-bot",
            DueDate = new DateTimeOffset(2026, 12, 1, 0, 0, 0, TimeSpan.Zero),
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var task = await response.Content.ReadFromJsonAsync<TaskResponse>();

        Assert.NotNull(task);
        Assert.Equal(title, task!.Title);
        Assert.Equal("pendiente", task.Status);
        Assert.Equal("media", task.Priority);
        Assert.NotEqual(Guid.Empty, task.Id);

        _created.Add(task.Id);
    }

    [DatabaseFact]
    public async Task Create_WithInvalidPayload_Returns400WithMessages()
    {
        var response = await _client.PostAsJsonAsync("/tasks", new CreateTaskRequest
        {
            Title = "ab",
            Assignee = "",
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();

        Assert.NotNull(error);
        Assert.Equal(StatusCodes.Status400BadRequest, error!.StatusCode);
        var messages = error.Message as string[] ?? Array.Empty<string>();

        Assert.Contains(messages, message => message.Contains("title"));
    }

    [DatabaseFact]
    public async Task Get_Returns200AfterCreate()
    {
        var created = await CreateTaskAsync("detalle");

        var response = await _client.GetAsync($"/tasks/{created.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var task = await response.Content.ReadFromJsonAsync<TaskResponse>();

        Assert.Equal(created.Id, task!.Id);
        Assert.Equal("detalle", task.Title);
    }

    [DatabaseFact]
    public async Task Get_WithUnknownId_Returns404ErrorResponse()
    {
        var response = await _client.GetAsync($"/tasks/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();

        Assert.NotNull(error);
        Assert.Equal("Not Found", error!.Error);
        Assert.Equal("Task not found", error.Message);
    }

    [DatabaseFact]
    public async Task List_FiltersByStatus()
    {
        var pending = await CreateTaskAsync("filtro-pendiente");
        var done = await CreateTaskAsync("filtro-hecha");

        await PatchTaskAsync(done.Id, new UpdateTaskRequest { Status = "hecha" });

        var response = await _client.GetAsync($"/tasks?status=hecha&limit=100");
        var payload = await response.Content.ReadFromJsonAsync<PaginatedTasksResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.All(payload!.Data, task => Assert.Equal("hecha", task.Status));
        Assert.Contains(payload.Data, task => task.Id == done.Id);
        Assert.DoesNotContain(payload.Data, task => task.Id == pending.Id);
    }

    [DatabaseFact]
    public async Task List_WithInvalidFilter_Returns400()
    {
        var response = await _client.GetAsync("/tasks?status=inexistente");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();

        Assert.NotNull(error);
        Assert.Equal(StatusCodes.Status400BadRequest, error!.StatusCode);
    }

    [DatabaseFact]
    public async Task List_Paginates()
    {
        var created = new List<TaskResponse>();
        for (var i = 0; i < 3; i++)
        {
            created.Add(await CreateTaskAsync($"paginacion-{Guid.NewGuid():N}"));
        }

        var response = await _client.GetAsync("/tasks?page=1&limit=2");
        var payload = await response.Content.ReadFromJsonAsync<PaginatedTasksResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.True(payload!.Data.Count <= 2);
        Assert.Equal(2, payload.Meta.Limit);
        Assert.Equal(1, payload.Meta.Page);
        Assert.True(payload.Meta.Total >= created.Count);
        Assert.True(payload.Meta.LastPage >= 2);
    }

    [DatabaseFact]
    public async Task Update_WithPatch_Returns200WithChanges()
    {
        var created = await CreateTaskAsync("parche");

        var response = await PatchTaskAsync(created.Id, new UpdateTaskRequest
        {
            Title = "parche-actualizado",
            Status = "en_curso",
            Priority = "alta",
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var task = await response.Content.ReadFromJsonAsync<TaskResponse>();

        Assert.NotNull(task);
        Assert.Equal("parche-actualizado", task!.Title);
        Assert.Equal("en_curso", task.Status);
        Assert.Equal("alta", task.Priority);
    }

    [DatabaseFact]
    public async Task Update_WithUnknownId_Returns404()
    {
        var response = await PatchTaskAsync(Guid.NewGuid(), new UpdateTaskRequest { Title = "nada" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [DatabaseFact]
    public async Task Delete_RemovesTask()
    {
        var created = await CreateTaskAsync("borrado");

        var deleted = await _client.DeleteAsync($"/tasks/{created.Id}");
        var fetched = await _client.GetAsync($"/tasks/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, fetched.StatusCode);

        _created.Remove(created.Id);
    }

    private async Task<TaskResponse> CreateTaskAsync(string title)
    {
        var response = await _client.PostAsJsonAsync("/tasks", new CreateTaskRequest
        {
            Title = title,
            Assignee = "qa-bot",
            DueDate = new DateTimeOffset(2026, 12, 1, 0, 0, 0, TimeSpan.Zero),
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var task = await response.Content.ReadFromJsonAsync<TaskResponse>();

        Assert.NotNull(task);
        _created.Add(task!.Id);

        return task;
    }

    private async Task<HttpResponseMessage> PatchTaskAsync(Guid id, UpdateTaskRequest request)
        => await _client.PatchAsJsonAsync($"/tasks/{id}", request);
}