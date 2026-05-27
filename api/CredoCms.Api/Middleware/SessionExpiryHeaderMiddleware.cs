using System.Globalization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace CredoCms.Api.Middleware;

/// <summary>
/// On authenticated 2xx responses, stamps an <c>X-Session-Expires-At</c> header
/// containing the auth ticket's UTC expiry. The SPA reads this to (re-)arm its
/// 5-minute session-expiry warning timer without needing a separate round-trip.
/// </summary>
public sealed class SessionExpiryHeaderMiddleware
{
    public const string HeaderName = "X-Session-Expires-At";

    private readonly RequestDelegate _next;

    public SessionExpiryHeaderMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(async () =>
        {
            if (context.User?.Identity?.IsAuthenticated != true)
            {
                return;
            }
            if (context.Response.StatusCode is < 200 or >= 300)
            {
                return;
            }
            if (context.Response.Headers.ContainsKey(HeaderName))
            {
                return;
            }

            var auth = await context.AuthenticateAsync(IdentityConstants.ApplicationScheme);
            if (auth?.Properties?.ExpiresUtc is { } exp)
            {
                context.Response.Headers[HeaderName] = exp.UtcDateTime.ToString("O", CultureInfo.InvariantCulture);
            }
        });

        await _next(context);
    }
}
