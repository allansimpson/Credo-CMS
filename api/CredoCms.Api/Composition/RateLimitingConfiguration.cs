using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace CredoCms.Api.Composition;

internal static class RateLimitingConfiguration
{
    public static void AddApiRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy("login", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                    }));

            options.AddPolicy("forgot-password", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        Window = TimeSpan.FromHours(1),
                        QueueLimit = 0,
                    }));

            options.AddPolicy("reset-password", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        Window = TimeSpan.FromHours(1),
                        QueueLimit = 0,
                    }));

            // Event registration: 5 submissions per IP per 10 minutes.
            // Honeypot + time-to-submit defenses also apply server-side.
            options.AddPolicy("event-register", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        Window = TimeSpan.FromMinutes(10),
                        QueueLimit = 0,
                    }));

            // Connect card: sliding window so a burst right before the hour
            // boundary doesn't reset cleanly. Plus Turnstile + honeypot +
            // 5s time-to-submit at the service layer.
            options.AddPolicy(
                CredoCms.Api.Controllers.PublicConnectCardController.RateLimitPolicy,
                httpContext =>
                    RateLimitPartition.GetSlidingWindowLimiter(
                        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        factory: _ => new SlidingWindowRateLimiterOptions
                        {
                            PermitLimit = 5,
                            Window = TimeSpan.FromHours(1),
                            SegmentsPerWindow = 6,
                            QueueLimit = 0,
                        }));
        });
    }
}
