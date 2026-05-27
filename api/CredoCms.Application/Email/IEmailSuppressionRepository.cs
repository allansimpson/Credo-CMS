using CredoCms.Application.Common;
using CredoCms.Domain.Email;

namespace CredoCms.Application.Email;

public interface IEmailSuppressionRepository
{
    Task<EmailSuppression?> GetByEmailAsync(string emailAddress, CancellationToken ct = default);

    /// <summary>Bulk lookup — used by the recipient resolver to filter a
    /// resolved list in a single query.</summary>
    Task<IReadOnlyDictionary<string, EmailSuppression>> BulkLookupAsync(
        IReadOnlyCollection<string> emailAddresses,
        CancellationToken ct = default);

    Task<PagedResult<EmailSuppression>> ListAsync(
        string? search,
        SuppressionType? type,
        int page,
        int pageSize,
        CancellationToken ct = default);

    /// <summary>Insert-or-no-op: if the address is already suppressed, the
    /// existing row stays (the original suppression context is more
    /// informative than the latest event).</summary>
    Task UpsertAsync(EmailSuppression suppression, CancellationToken ct = default);

    Task RemoveAsync(string emailAddress, CancellationToken ct = default);
}
