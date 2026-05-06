using CredoCms.Application.Email;
using CredoCms.Application.Common;
using CredoCms.Domain.Common;
using CredoCms.Domain.Email;
using CredoCms.Domain.Groups;
using CredoCms.Domain.Identity;
using CredoCms.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CredoCms.Infrastructure.Email;

public sealed class RecipientResolver : IRecipientResolver
{
    private readonly ApplicationDbContext _db;
    private readonly IEmailSuppressionService _suppression;

    public RecipientResolver(ApplicationDbContext db, IEmailSuppressionService suppression)
    {
        _db = db;
        _suppression = suppression;
    }

    public async Task<IReadOnlyList<EmailRecipient>> ResolveAsync(
        BroadcastTargetMode targetMode,
        IReadOnlyCollection<Guid>? targetGroupIds,
        EmailCategory category,
        CancellationToken ct = default)
    {
        var users = await BaseUserQueryAsync(targetMode, targetGroupIds, ct).ConfigureAwait(false);
        if (users.Count == 0) return Array.Empty<EmailRecipient>();

        // Apply category-specific preference filter — non-transactional
        // categories respect the per-user opt-out.
        users = users.Where(u => RespectsPreference(u, category)).ToList();

        // Bulk-lookup suppression list once for the whole resolved set.
        var emails = users.Select(u => u.Email!).ToList();
        var suppressed = await _suppression.BulkLookupAsync(emails, ct).ConfigureAwait(false);

        var recipients = new List<EmailRecipient>(users.Count);
        foreach (var u in users)
        {
            if (suppressed.Contains(u.Email!.ToLowerInvariant())) continue;
            recipients.Add(BuildRecipient(u));
        }
        return recipients;
    }

    public async Task<RecipientPreview> PreviewAsync(
        BroadcastTargetMode targetMode,
        IReadOnlyCollection<Guid>? targetGroupIds,
        EmailCategory category,
        int sampleSize = 8,
        CancellationToken ct = default)
    {
        var resolved = await ResolveAsync(targetMode, targetGroupIds, category, ct).ConfigureAwait(false);
        var sample = resolved.Take(sampleSize)
            .Select(r => new RecipientPreviewItem(r.Name, r.Address))
            .ToList();
        return new RecipientPreview(resolved.Count, sample);
    }

    private async Task<List<ApplicationUser>> BaseUserQueryAsync(
        BroadcastTargetMode mode,
        IReadOnlyCollection<Guid>? groupIds,
        CancellationToken ct)
    {
        // Active users with non-empty email. The "All Members" path
        // picks every user in the Member/Editor/Administrator role.
        // The "Specific Groups" path takes the union of active members
        // of the chosen groups, then subtracts users who set the
        // per-group ReceiveGroupEmails=false.
        if (mode == BroadcastTargetMode.AllMembers)
        {
            return await _db.Users
                .AsNoTracking()
                .Where(u => u.IsActive && u.Email != null)
                .ToListAsync(ct).ConfigureAwait(false);
        }

        if (groupIds is null || groupIds.Count == 0) return new();

        var hits = await (
            from m in _db.GroupMemberships.AsNoTracking()
            join u in _db.Users.AsNoTracking() on m.UserId equals u.Id
            where groupIds.Contains(m.GroupId)
                && m.Status == GroupMembershipStatus.Active
                && u.IsActive
                && u.Email != null
                // Per-group override: false explicitly excludes; null/true include.
                && (m.ReceiveGroupEmails == null || m.ReceiveGroupEmails == true)
            select u)
            .Distinct()
            .ToListAsync(ct).ConfigureAwait(false);
        return hits;
    }

    /// <summary>Per-category preference filter.
    /// <see cref="EmailCategory.Transactional"/> bypasses preferences and
    /// suppression, so it never reaches this method (the resolver isn't
    /// invoked for transactional sends).</summary>
    private static bool RespectsPreference(ApplicationUser u, EmailCategory category) => category switch
    {
        EmailCategory.News => u.ReceiveNewsEmails,
        EmailCategory.Blog => u.ReceiveBlogEmails,
        EmailCategory.Broadcast => u.ReceiveBroadcastEmails,
        EmailCategory.GroupCommunication => u.ReceiveGroupEmailsGlobal,
        _ => true,
    };

    private static EmailRecipient BuildRecipient(ApplicationUser u)
    {
        var merge = new Dictionary<string, string>
        {
            ["firstName"] = u.FirstName,
            ["lastName"] = u.LastName,
            ["displayName"] = u.DisplayName,
        };
        return new EmailRecipient(u.Email!, u.DisplayName, u.Id, merge);
    }
}
