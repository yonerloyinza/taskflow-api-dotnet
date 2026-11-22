namespace TaskFlow.Api.Middleware;

/// <summary>
/// Cabeceras de seguridad mínimas. Traefik termina TLS, así que HSTS se emite
/// únicamente cuando la petición llega ya cifrada al contenedor.
/// Swagger UI se sirve sin CSP porque carga estilos/scripts desde su CDN.
/// </summary>
public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next) => _next = next;

    public Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;
        var path = context.Request.Path.Value ?? "/";
        var isSwagger = path.StartsWith("/docs", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase);

        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "DENY";
        headers["Referrer-Policy"] = "no-referrer";
        headers["Cross-Origin-Resource-Policy"] = "same-site";
        headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";

        if (!isSwagger)
        {
            headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";
        }

        if (context.Request.IsHttps)
        {
            headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
        }

        return _next(context);
    }
}