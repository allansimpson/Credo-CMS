using CredoCms.Application.Common;

namespace CredoCms.Application.UserManagement;

public interface IUserAdminService
{
    Task<PagedResult<UserListItemDto>> ListAsync(UserListQuery query, CancellationToken ct = default);
    Task<UserDetailDto?> GetAsync(Guid id, CancellationToken ct = default);
    Task<UserMutationResult> CreateAsync(CreateUserRequest request, CancellationToken ct = default);
    Task<UserMutationResult> UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken ct = default);
    Task<UserMutationResult> DeactivateAsync(Guid id, CancellationToken ct = default);
    Task<UserMutationResult> ReactivateAsync(Guid id, CancellationToken ct = default);
    Task<UserMutationResult> ForceLogoutAsync(Guid id, CancellationToken ct = default);
    Task<UserMutationResult> SendPasswordResetEmailAsync(Guid id, CancellationToken ct = default);
    Task<UserMutationResult> HardDeleteAsync(Guid id, HardDeleteUserRequest request, CancellationToken ct = default);

    /// <summary>Admin override for the per-user profile fields. Bypasses the
    /// per-user guard <see cref="Profile.IProfileService"/> enforces — admins
    /// can edit anyone's profile by design.</summary>
    Task<UserMutationResult> UpdateProfileFieldsAsync(Guid id, UpdateUserProfileFieldsRequest request, CancellationToken ct = default);

    /// <summary>Resets the four notification-preference flags to their
    /// defaults (News=on, Blog=off, Broadcast=on, GroupGlobal=on).</summary>
    Task<UserMutationResult> ResetNotificationsAsync(Guid id, CancellationToken ct = default);

    /// <summary>Aggregate counts for the admin user-detail screen.</summary>
    Task<AdminUserNotesDto?> GetAdminNotesAsync(Guid id, CancellationToken ct = default);
}
