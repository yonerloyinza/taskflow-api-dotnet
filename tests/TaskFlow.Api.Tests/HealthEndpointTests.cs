using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using TaskFlow.Api.Contracts;

namespace TaskFlow.Api.Tests;

public sealed class HealthEndpointTests
{
    [Fact]
    public async Task Health_WhenDatabaseIsUp_ReturnsContractPayload()
    {
        using var factory = new TaskFlowApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json; charset=utf-8", response.Content.Headers.ContentType?.ToString());

        var payload = await response.Content.ReadFromJsonAsync<HealthResponse>();

        Assert.NotNull(payload);
        Assert.Equal("ok", payload!.Status);
        Assert.Equal("taskflow-api-csharp", payload.Service);
        Assert.Equal("up", payload.Database);
        Assert.NotEqual(default, payload.Timestamp);
    }

    [Fact]
    public async Task Health_WhenDatabaseIsDown_Returns503ErrorResponse()
    {
        using var factory = new TaskFlowApiFactory { DatabaseAvailable = false };
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();

        Assert.NotNull(payload);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, payload!.StatusCode);
        Assert.Equal("Service Unavailable", payload.Error);
        Assert.Equal("/health", payload.Path);
        Assert.Contains("Database", payload.Message?.ToString());
    }

    [Fact]
    public async Task Health_IncludesSecurityHeaders()
    {
        using var factory = new TaskFlowApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
        Assert.Equal("no-referrer", response.Headers.GetValues("Referrer-Policy").Single());
    }
}