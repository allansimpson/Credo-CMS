using CredoCms.Application.Common;
using CredoCms.Application.UserManagement;
using CredoCms.Domain.Identity;
using CredoCms.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CredoCms.Infrastructure.UserManagement;

/// <summary>
/// Read-side user queries that join into Identity tables to project list/detail
/// DTOs with role membership in a single round trip.
/// </summary>
public sealed class UserAdminQueries : IUserAdminQueries
{
    private readonly ApplicationDbContext _db;

    public UserAdminQueries(ApplicationDbContext db) => _db = db;

    public async Task<PagedResult<UserListItemDto>> ListAsync(UserListQuery query, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var users = _db.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.Trim();
            users = users.Where(u =>
                EF.Functions.Like(u.FirstName, $"%{s}%") ||
                EF.Functions.Like(u.LastName, $"%{s}%") ||
                EF.Functions.Like(u.Email!, $"%{s}%"));
        }

        if (query.IsActive is { } active)
        {
            users = users.Where(u => u.IsActive == active);
        }

        if (!string.IsNullOrWhiteSpace(query.Role))
        {
            var roleName = query.Role;

            users = from u in users
                    join ur in _db.UserRoles on u.Id equals ur.UserId
                    join r in _db.Roles on ur.RoleId equals r.Id
                    where r.Name == roleName
                    select u;
        }

        var totalCount = await users.CountAsync(ct).ConfigureAwait(false);

        var page = await users
            .OrderBy(u => u.LastName).ThenBy(u => u.FirstName)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        // Hydrate each user's roles in a single follow-up query.
        var userIds = page.Select(u => u.Id).ToList();
        var roleAssignments = await (
            from ur in _db.UserRoles.AsNoTracking()
            where userIds.Contains(ur.UserId)
            join r in _db.Roles.AsNoTracking() on ur.RoleId equals r.Id
            select new { ur.UserId, r.Name }
        ).ToListAsync(ct).ConfigureAwait(false);

        var roleLookup = roleAssignments
            .GroupBy(x => x.UserId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<string>)[.. g.Select(x => x.Name!).Order()]);

        var items = page.Select(u => new UserListItemDto(
            u.Id,
            u.Email ?? string.Empty,
            u.FirstName,
            u.LastName,
            u.DisplayName,
            u.IsActive,
            u.EmailConfirmed,
            u.CreatedAt,
            u.LastLoginAt,
            roleLookup.TryGetValue(u.Id, out var roles) ? roles : Array.Empty<string>())
        ).ToList();

        return new PagedResult<UserListItemDto>(items, totalCount, query.Page, query.PageSize);
    }

    public async Task<UserDetailDto?> GetAsync(Guid id, CancellationToken ct = default)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, ct).ConfigureAwait(false);
        if (user is null)
        {
            return null;
        }

        var roles = await (
            from ur in _db.UserRoles.AsNoTracking()
            where ur.UserId == id
            join r in _db.Roles.AsNoTracking() on ur.RoleId equals r.Id
            select r.Name!
        ).OrderBy(n => n).ToListAsync(ct).ConfigureAwait(false);

        return new UserDetailDto(
            user.Id,
            user.Email ?? string.Empty,
            user.FirstName,
            user.LastName,
            user.DisplayName,
            user.IsActive,
            user.EmailConfirmed,
            user.LockoutEnabled,
            user.LockoutEnd,
            user.CreatedAt,
            user.LastLoginAt,
            roles);
    }
}
