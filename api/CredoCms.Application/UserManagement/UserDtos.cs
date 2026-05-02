using CredoCms.Application.Common;

namespace CredoCms.Application.UserManagement;

public sealed record UserListItemDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string DisplayName,
    bool IsActive,
    bool EmailConfirmed,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastLoginAt,
    IReadOnlyList<string> Roles);

public sealed record UserDetailDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string DisplayName,
    bool IsActive,
    bool EmailConfirmed,
    bool LockoutEnabled,
    DateTimeOffset? LockoutEndUtc,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastLoginAt,
    IReadOnlyList<string> Roles);

public sealed record CreateUserRequest(
    string Email,
    string FirstName,
    string LastName,
    IReadOnlyList<string> Roles,
    bool SendInvitation);

public sealed record UpdateUserRequest(
    string Email,
    string FirstName,
    string LastName,
    IReadOnlyList<string> Roles,
    bool IsActive);

public sealed record UserListQuery(
    string? Search = null,
    string? Role = null,
    bool? IsActive = null,
    int Page = 1,
    int PageSize = 50);

public sealed record UserMutationResult(bool Succeeded, IReadOnlyList<string> Errors, UserDetailDto? User = null)
{
    public static UserMutationResult Success(UserDetailDto user) => new(true, Array.Empty<string>(), user);
    public static UserMutationResult Failure(params string[] errors) => new(false, errors, null);
}

public sealed record HardDeleteUserRequest(string ConfirmDisplayName);
