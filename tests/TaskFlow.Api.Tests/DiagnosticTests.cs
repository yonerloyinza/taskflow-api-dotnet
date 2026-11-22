using Xunit;
using System.Net;

namespace TaskFlow.Api.Tests;

public sealed class DiagnosticTests
{
    [Fact]
    public async Task DumpHealth()
    {
        using var factory = new TaskFlowApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Fail($"status={(int)response.StatusCode} body={body}");
    }
}