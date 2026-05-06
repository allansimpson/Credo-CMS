using CredoCms.Application.Common;

namespace CredoCms.Application.Members;

/// <summary>
/// Service for the authenticated members directory at <c>/api/members</c>.
/// Both methods enforce the directory opt-in gate (<c>IsListedInDirectory &amp;&amp;
/// IsActive</c>) and apply field-level privacy filtering before returning DTOs.
/// </summary>
public interface IMembersDirectoryService
{
    Task<PagedResult<MemberListItemDto>> ListAsync(
        MembersDirectoryQuery query,
        CancellationToken ct = default);

    /// <summary>
    /// Returns the member detail for <paramref name="userId"/>, or null if the
    /// member is not listed (deactivated, opted out, or doesn't exist). The
    /// controller surfaces a 404 in either case so the public API can't be
    /// used to probe whether a particular user account exists.
    /// </summary>
    Task<MemberDetailDto?> GetByIdAsync(Guid userId, CancellationToken ct = default);
}
