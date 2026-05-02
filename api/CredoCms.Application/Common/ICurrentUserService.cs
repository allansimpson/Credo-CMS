namespace CredoCms.Application.Common;

/// <summary>
/// Resolves "who is making this request" for cross-cutting concerns: the versioning
/// interceptor stamps writes with <see cref="UserId"/>, the audit logger captures
/// <see cref="DisplayName"/> and <see cref="IpAddress"/>, etc.
/// Implementations live in Infrastructure (one for HTTP-context-bound calls and one
/// fallback for background-service writes).
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// The acting user's id. Falls back to the System User id when there is no
    /// authenticated principal (e.g., background services, anonymous endpoints).
    /// </summary>
    Guid UserId { get; }

    /// <summary>
    /// The acting user's display name. "System" for unauthenticated/system actions.
    /// </summary>
    string DisplayName { get; }

    /// <summary>The acting user's IP address, when available.</summary>
    string? IpAddress { get; }

    bool IsAuthenticated { get; }

    /// <summary>The set of role names the acting user holds.</summary>
    IReadOnlyCollection<string> Roles { get; }
}
