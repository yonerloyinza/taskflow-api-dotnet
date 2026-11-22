using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TaskFlow.Application.Abstractions;

namespace TaskFlow.Api.Tests;

/// <summary>
/// Arranca la API real sin red: sustituye el sondeo de PostgreSQL por un doble
/// y desactiva el rate limiter para que la suite sea determinista.
/// </summary>
public sealed class TaskFlowApiFactory : WebApplicationFactory<Program>
{
    public bool DatabaseAvailable { get; set; } = true;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("CORS_ORIGIN", "https://taskflow-api-dotnet.xprad.com");
        builder.UseSetting("RATE_LIMIT_ENABLED", "false");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IDatabaseHealthChecker>();
            var available = DatabaseAvailable;
            services.AddSingleton<IDatabaseHealthChecker>(new FakeDatabaseHealthChecker(() => available));
        });
    }

    private sealed class FakeDatabaseHealthChecker : IDatabaseHealthChecker
    {
        private readonly Func<bool> _available;

        public FakeDatabaseHealthChecker(Func<bool> available) => _available = available;

        public Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default) => Task.FromResult(_available());
    }
}