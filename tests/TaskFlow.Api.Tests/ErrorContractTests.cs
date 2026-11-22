using System.Net;
using Xunit;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using TaskFlow.Api.Contracts;

namespace TaskFlow.Api.Tests;

public sealed class ErrorContractTests
{
    [Fact]
    public async Task UnknownRoute_Returns404ErrorResponse()
    {
        using var factory = new TaskFlowApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/tasks/does-not-exist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();

        Assert.NotNull(payload);
        Assert.Equal(StatusCodes.Status404NotFound, payload!.StatusCode);
        Assert.Equal("Not Found", payload.Error);
        Assert.Equal("/tasks/does-not-exist", payload.Path);
        Assert.False(string.IsNullOrWhiteSpace(payload.Message?.ToString()));
    }

    [Fact]
    public async Task WrongMethodOnHealth_Returns405ErrorResponse()
    {
        using var factory = new TaskFlowApiFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsync("/health", content: null);

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();

        Assert.NotNull(payload);
        Assert.Equal(StatusCodes.Status405MethodNotAllowed, payload!.StatusCode);
    }

    [Fact]
    public async Task Health_AllowsConfiguredCorsOrigin()
    {
        using var factory = new TaskFlowApiFactory();
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health");

        request.Headers.Add("Origin", "https://taskflow-api-dotnet.xprad.com");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.Contains("Access-Control-Allow-Origin"));
        Assert.Equal("https://taskflow-api-dotnet.xprad.com", response.Headers.GetValues("Access-Control-Allow-Origin").Single());
    }
}