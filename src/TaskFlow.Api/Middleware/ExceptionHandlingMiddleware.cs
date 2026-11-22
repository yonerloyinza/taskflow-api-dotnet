using FluentValidation;
using TaskFlow.Api.Contracts;

namespace TaskFlow.Api.Middleware;

/// <summary>
/// Traduce excepciones no controladas al formato `ErrorResponse` del contrato
/// y convierte los 404/405 sin cuerpo en errores JSON coherentes.
/// Los errores de validación (FluentValidation) devuelven 400 con lista de mensajes.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);

            if ((context.Response.StatusCode is StatusCodes.Status404NotFound or StatusCodes.Status405MethodNotAllowed)
                && context.Response.ContentLength is null or 0
                && !context.Response.HasStarted)
            {
                var statusText = context.Response.StatusCode == StatusCodes.Status404NotFound ? "Not Found" : "Method Not Allowed";
                await WriteAsync(context, context.Response.StatusCode, statusText, statusText);
            }
        }
        catch (ValidationException exception)
        {
            var messages = exception.Errors
                .Select(error => error.ErrorMessage)
                .ToArray();

            await WriteAsync(context, StatusCodes.Status400BadRequest, "Bad Request", messages);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unhandled exception on {Method} {Path}", context.Request.Method, context.Request.Path);
            await WriteAsync(context, StatusCodes.Status500InternalServerError, "Internal Server Error", "Internal server error");
        }
    }

    private static async Task WriteAsync(HttpContext context, int statusCode, string error, object message)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.Clear();
        context.Response.StatusCode = statusCode;

        var payload = new ErrorResponse
        {
            StatusCode = statusCode,
            Error = error,
            Message = message,
            Timestamp = DateTimeOffset.UtcNow,
            Path = context.Request.Path.Value ?? "/",
        };

        await context.Response.WriteAsJsonAsync(payload);
    }
}