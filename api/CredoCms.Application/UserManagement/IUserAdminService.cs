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
}
