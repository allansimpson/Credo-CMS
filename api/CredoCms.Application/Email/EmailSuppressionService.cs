using CredoCms.Application.Common;
using CredoCms.Domain.Email;

namespace CredoCms.Application.Email;

public sealed class EmailSuppressionService : IEmailSuppressionService
{
    private readonly IEmailSuppressionRepository _repo;
    private readonly IAuditLogger _audit;

    public EmailSuppressionService(IEmailSuppressionRepository repo, IAuditLogger audit)
    {
        _repo = repo;
        _audit = audit;
    }

    public async Task<bool> IsSuppressedAsync(string emailAddress, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(emailAddress)) return false;
        var hit = await _repo.GetByEmailAsync(Normalize(emailAddress), ct).ConfigureAwait(false);
        return hit is not null;
    }

    public async Task<IReadOnlySet<string>> BulkLookupAsync(
        IReadOnlyCollection<string> emailAddresses,
        CancellationToken ct = default)
    {
        if (emailAddresses.Count == 0) return new HashSet<string>();
        var normalized = emailAddresses
            .Where(a => !string.IsNullOrWhiteSpace(a))
            .Select(Normalize)
            .ToHashSet();
        if (normalized.Count == 0) return new HashSet<string>();
        var hits = await _repo.BulkLookupAsync(normalized, ct).ConfigureAwait(false);
        return hits.Keys.ToHashSet();
    }

    public Task<PagedResult<EmailSuppression>> ListAsync(
        string? search,
        SuppressionType? type,
        int page,
        int pageSize,
        CancellationToken ct = default)
        => _repo.ListAsync(search, type, page, pageSize, ct);

    public async Task AddAsync(
        string emailAddress,
        SuppressionType type,
        SuppressionSource source,
        string? reason,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(emailAddress);
        var record = new EmailSuppression
        {
            Id = Guid.NewGuid(),
            EmailAddress = Normalize(emailAddress),
            SuppressionType = type,
            CreatedSource = source,
            Reason = reason,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        await _repo.UpsertAsync(record, ct).ConfigureAwait(false);
        await _audit.WriteAsync(
            "EmailSuppression.Added",
            nameof(EmailSuppression),
            entityId: record.EmailAddress,
            details: new { record.SuppressionType, record.CreatedSource, record.Reason },
            cancellationToken: ct).ConfigureAwait(false);
    }

    public async Task RemoveAsync(string emailAddress, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(emailAddress);
        var normalized = Normalize(emailAddress);
        await _repo.RemoveAsync(normalized, ct).ConfigureAwait(false);
        await _audit.WriteAsync(
            "EmailSuppression.Removed",
            nameof(EmailSuppression),
            entityId: normalized,
            cancellationToken: ct).ConfigureAwait(false);
    }

    /// <summary>Stored representation: lowercase + trimmed. Mirrored at the
    /// DB column index (see <c>EmailSuppressionConfiguration</c>).</summary>
    internal static string Normalize(string address) => address.Trim().ToLowerInvariant();
}
