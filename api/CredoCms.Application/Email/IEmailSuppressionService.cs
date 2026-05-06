using CredoCms.Application.Common;
using CredoCms.Domain.Email;

namespace CredoCms.Application.Email;

/// <summary>
/// Application-facing service for managing the email suppression list. Every
/// admin-driven mutation writes to the audit log. Reads are inexpensive —
/// individual lookups are a single indexed query, bulk lookups are one
/// <c>WHERE EmailAddress IN (...)</c> query intended for the broadcast
/// recipient resolver.
/// </summary>
public interface IEmailSuppressionService
{
    Task<bool> IsSuppressedAsync(string emailAddress, CancellationToken ct = default);

    /// <summary>Bulk-resolve a recipient list. Returns the lowercased
    /// addresses found in the suppression list (the caller filters its
    /// list against the returned set).</summary>
    Task<IReadOnlySet<string>> BulkLookupAsync(
        IReadOnlyCollection<string> emailAddresses,
        CancellationToken ct = default);

    Task<PagedResult<EmailSuppression>> ListAsync(
        string? search,
        SuppressionType? type,
        int page,
        int pageSize,
        CancellationToken ct = default);

    /// <summary>Insert-or-no-op. Used by the SendGrid webhook (hard
    /// bounce, spam complaint, unsubscribe), the one-click unsubscribe
    /// endpoint, and the admin manual-add UI. Audit-logged.</summary>
    Task AddAsync(
        string emailAddress,
        SuppressionType type,
        SuppressionSource source,
        string? reason,
        CancellationToken ct = default);

    /// <summary>Admin-only: remove an address from the suppression list.
    /// CAN-SPAM compliance is the operator's responsibility — this is a
    /// rare admin action. Audit-logged.</summary>
    Task RemoveAsync(string emailAddress, CancellationToken ct = default);
}
