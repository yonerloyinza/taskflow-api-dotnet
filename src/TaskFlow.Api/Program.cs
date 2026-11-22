using System.Threading.RateLimiter;
using Microsoft.AspNetCore.RateLimiting;
using TaskFlow.Api.Contracts;
using TaskFlow.Api.Middleware;
using TaskFlow.Application;
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
}));

ConfigureRateLimiting(builder);

var app = builder.Build();

app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseCors();
app.UseRateLimiter();

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

            return context.HttpContext.Response.WriteAsJsonAsync(payload, cancellationToken: cancellationToken);
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

/// <summary>Expuesto para WebApplicationFactory&lt;Program&gt; en los tests de integración.</summary>
public partial class Program;