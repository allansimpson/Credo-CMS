using CredoCms.Application.Common;

namespace CredoCms.Application.Members;

/// <summary>
/// Members directory service. Reads come from <see cref="IMembersDirectoryQueries"/>
/// (Infrastructure). The privacy filter — null-out fields the member didn't opt
/// in to share — lives here so it cannot be bypassed by a different repo
/// implementation. The query layer returns the raw row + opt-in flags; this
/// layer turns those into a public-safe DTO.
/// </summary>
public sealed class MembersDirectoryService : IMembersDirectoryService
{
    private readonly IMembersDirectoryQueries _queries;
    public MembersDirectoryService(IMembersDirectoryQueries queries) => _queries = queries;

    public async Task<PagedResult<MemberListItemDto>> ListAsync(
        MembersDirectoryQuery query,
        CancellationToken ct = default)
    {
        var safe = query with
        {
            Page = Math.Max(1, query.Page),
            PageSize = Math.Clamp(query.PageSize, 1, 100),
        };
        var page = await _queries.ListAsync(safe, ct).ConfigureAwait(false);

        var items = page.Items.Select(r => new MemberListItemDto(
            r.UserId,
            r.FirstName,
            r.LastName,
            $"{r.FirstName} {r.LastName}".Trim(),
            r.ShowEmailInDirectory ? r.Email : null,
            r.ShowPhoneInDirectory ? r.PhoneNumber : null,
            r.ShowPhotoInDirectory ? r.PhotoBlobUrl : null,
            r.ShowPhotoInDirectory ? r.PhotoWebpBlobUrl : null,
            r.ShowPhotoInDirectory ? r.PhotoAltText : null
        )).ToList();

        return new PagedResult<MemberListItemDto>(items, page.TotalCount, safe.Page, safe.PageSize);
    }

    public async Task<MemberDetailDto?> GetByIdAsync(Guid userId, CancellationToken ct = default)
    {
        var row = await _queries.GetByIdAsync(userId, ct).ConfigureAwait(false);
        if (row is null) return null;

        var memberships = await _queries.ListVisibleGroupsForUserAsync(userId, ct).ConfigureAwait(false);

        return new MemberDetailDto(
            row.UserId,
            row.FirstName,
            row.LastName,
            $"{row.FirstName} {row.LastName}".Trim(),
            row.ShowEmailInDirectory ? row.Email : null,
            row.ShowPhoneInDirectory ? row.PhoneNumber : null,
            row.ShowAddressInDirectory ? row.AddressLine1 : null,
            row.ShowAddressInDirectory ? row.AddressLine2 : null,
            row.ShowAddressInDirectory ? row.City : null,
            row.ShowAddressInDirectory ? row.StateOrRegion : null,
            row.ShowAddressInDirectory ? row.PostalCode : null,
            row.ShowAddressInDirectory ? row.Country : null,
            row.ShowPhotoInDirectory ? row.PhotoBlobUrl : null,
            row.ShowPhotoInDirectory ? row.PhotoWebpBlobUrl : null,
            row.ShowPhotoInDirectory ? row.PhotoAltText : null,
            row.PublicAuthorBio,
            memberships);
    }
}

/// <summary>
/// Read-side queries for the members directory. Implementation in Infrastructure
/// applies the directory opt-in gate (<c>IsListedInDirectory &amp;&amp;
/// IsActive</c>) at the database, so unlisted users are never returned.
/// </summary>
public interface IMembersDirectoryQueries
{
    Task<PagedResult<MemberDirectoryRow>> ListAsync(MembersDirectoryQuery query, CancellationToken ct = default);
    Task<MemberDirectoryRow?> GetByIdAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<MemberGroupMembershipDto>> ListVisibleGroupsForUserAsync(Guid userId, CancellationToken ct = default);
}

/// <summary>
/// Raw materialized row from the database. Includes opt-in flags so the
/// service layer can apply the privacy filter; not exposed beyond the
/// Application boundary.
/// </summary>
public sealed record MemberDirectoryRow(
    Guid UserId,
    string FirstName,
    string LastName,
    string? Email,
    string? PhoneNumber,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? StateOrRegion,
    string? PostalCode,
    string? Country,
    string? PhotoBlobUrl,
    string? PhotoWebpBlobUrl,
    string? PhotoAltText,
    string? PublicAuthorBio,
    bool ShowEmailInDirectory,
    bool ShowPhoneInDirectory,
    bool ShowAddressInDirectory,
    bool ShowPhotoInDirectory);
