using CredoCms.Application.Common;
using CredoCms.Application.Members;
using CredoCms.Domain.Groups;
using CredoCms.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CredoCms.Infrastructure.Members;

/// <summary>
/// EF-backed reads for the members directory. The opt-in gate
/// (<c>IsListedInDirectory &amp;&amp; IsActive</c>) is enforced here at the
/// database — the service layer cannot accidentally widen visibility because
/// unlisted users never make it past these queries.
/// </summary>
public sealed class MembersDirectoryQueries : IMembersDirectoryQueries
{
    private readonly ApplicationDbContext _db;
    public MembersDirectoryQueries(ApplicationDbContext db) => _db = db;

    public async Task<PagedResult<MemberDirectoryRow>> ListAsync(
        MembersDirectoryQuery query,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var users = _db.Users.AsNoTracking()
            .Where(u => u.IsListedInDirectory && u.IsActive);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.Trim();
            users = users.Where(u =>
                EF.Functions.Like(u.FirstName, $"%{s}%") ||
                EF.Functions.Like(u.LastName, $"%{s}%"));
        }

        var totalCount = await users.CountAsync(ct).ConfigureAwait(false);

        var page = await users
            .OrderBy(u => u.LastName).ThenBy(u => u.FirstName)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(u => new MemberDirectoryRow(
                u.Id,
                u.FirstName,
                u.LastName,
                u.Email,
                u.PhoneNumber,
                u.AddressLine1,
                u.AddressLine2,
                u.City,
                u.StateOrRegion,
                u.PostalCode,
                u.Country,
                u.PhotoBlobUrl,
                u.PhotoWebpBlobUrl,
                u.PhotoAltText,
                u.PublicAuthorBio,
                u.ShowEmailInDirectory,
                u.ShowPhoneInDirectory,
                u.ShowAddressInDirectory,
                u.ShowPhotoInDirectory))
            .ToListAsync(ct).ConfigureAwait(false);

        return new PagedResult<MemberDirectoryRow>(page, totalCount, query.Page, query.PageSize);
    }

    public Task<MemberDirectoryRow?> GetByIdAsync(Guid userId, CancellationToken ct = default) =>
        _db.Users.AsNoTracking()
            .Where(u => u.Id == userId && u.IsListedInDirectory && u.IsActive)
            .Select(u => new MemberDirectoryRow(
                u.Id,
                u.FirstName,
                u.LastName,
                u.Email,
                u.PhoneNumber,
                u.AddressLine1,
                u.AddressLine2,
                u.City,
                u.StateOrRegion,
                u.PostalCode,
                u.Country,
                u.PhotoBlobUrl,
                u.PhotoWebpBlobUrl,
                u.PhotoAltText,
                u.PublicAuthorBio,
                u.ShowEmailInDirectory,
                u.ShowPhoneInDirectory,
                u.ShowAddressInDirectory,
                u.ShowPhotoInDirectory))
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<MemberGroupMembershipDto>> ListVisibleGroupsForUserAsync(
        Guid userId, CancellationToken ct = default)
    {
        // Hidden groups never appear on a member's profile, even to other
        // members. The Group's own query filter excludes soft-deleted rows
        // already, so no extra check on Group.IsDeleted is needed.
        var rows = await (
            from m in _db.GroupMemberships.AsNoTracking()
            join g in _db.Groups.AsNoTracking() on m.GroupId equals g.Id
            where m.UserId == userId
                && m.Status == GroupMembershipStatus.Active
                && g.Visibility != GroupVisibility.Hidden
            orderby g.Name
            select new MemberGroupMembershipDto(g.Id, g.Slug, g.Name, m.IsLeader)
        ).ToListAsync(ct).ConfigureAwait(false);

        return rows;
    }
}
