using Microsoft.AspNetCore.Http;

namespace CredoCms.Api.Middleware;

/// <summary>
/// For protected admin/docs URL prefixes, converts a 403 Forbidden response into
/// a 404 Not Found. This is the API side of the "covert 404" pattern: callers
/// without sufficient role see "endpoint doesn't exist" rather than "you're not
/// allowed", which prevents endpoint enumeration.
///
/// 401s on member-area endpoints stay 401 so the SPA can detect session expiry.
/// </summary>
public sealed class ForbiddenToNotFoundMiddleware
{
    private static readonly string[] CovertPrefixes =
    [
        "/api/admin",
        "/api/docs",
    ];

    private readonly RequestDelegate _next;

    public ForbiddenToNotFoundMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        await _next(context);

        if (context.Response.StatusCode != StatusCodes.Status403Forbidden)
        {
            return;
        }

        if (!IsCovertPath(context.Request.Path))
        {
            return;
        }

        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.Clear();
        context.Response.StatusCode = StatusCodes.Status404NotFound;
    }

    private static bool IsCovertPath(PathString path)
    {
        foreach (var prefix in CovertPrefixes)
        {
            if (path.StartsWithSegments(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }
}
