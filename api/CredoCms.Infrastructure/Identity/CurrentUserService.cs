using System.Security.Claims;
using CredoCms.Application.Common;
using CredoCms.Domain.Common;
using Microsoft.AspNetCore.Http;

namespace CredoCms.Infrastructure.Identity;

/// <summary>
/// Reads the current user from <see cref="IHttpContextAccessor"/>. Falls back to the
/// System User identity when no HTTP context is present (background services) or when
/// the request is anonymous — this lets versioned-entity writes performed by jobs
/// still satisfy the NOT NULL <c>ModifiedByUserId</c> constraint.
/// </summary>
public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid UserId
    {
        get
        {
            var principal = _httpContextAccessor.HttpContext?.User;
            var idClaim = principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? principal?.FindFirst("sub")?.Value;
            return Guid.TryParse(idClaim, out var id) ? id : SystemConstants.SystemUserId;
        }
    }

    public string DisplayName
    {
        get
        {
            var principal = _httpContextAccessor.HttpContext?.User;
            var name = principal?.FindFirst("name")?.Value
                       ?? principal?.FindFirst(ClaimTypes.Name)?.Value
                       ?? principal?.Identity?.Name;
            return string.IsNullOrWhiteSpace(name)
                ? SystemConstants.SystemUserDisplayName
                : name;
        }
    }

    public string? IpAddress =>
        _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();

    public bool IsAuthenticated =>
        _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

    public IReadOnlyCollection<string> Roles
    {
        get
        {
            var principal = _httpContextAccessor.HttpContext?.User;
            if (principal is null)
            {
                return Array.Empty<string>();
            }

            return [.. principal.FindAll(ClaimTypes.Role).Select(c => c.Value)];
        }
    }
}
