using CredoCms.Application.Common;
using CredoCms.Domain.Email;

namespace CredoCms.Application.Email;

public interface IEmailTemplateService
{
    Task<EmailTemplate?> GetAsync(Guid id, CancellationToken ct = default);
    Task<EmailTemplate?> GetByKeyAsync(string key, CancellationToken ct = default);
    Task<List<EmailTemplate>> ListAsync(CancellationToken ct = default);

    /// <summary>Updates Subject + HtmlBody + PlainTextBody only. Mutating
    /// <see cref="EmailTemplate.TemplateKey"/> is forbidden — code paths
    /// look up by key, so renaming would break consumers.</summary>
    Task<EmailTemplate> UpdateAsync(Guid id, string subject, string htmlBody, string? plainTextBody, CancellationToken ct = default);
}

public sealed class EmailTemplateService : IEmailTemplateService
{
    private readonly IEmailTemplateRepository _repo;
    private readonly IAuditLogger _audit;

    public EmailTemplateService(IEmailTemplateRepository repo, IAuditLogger audit)
    {
        _repo = repo;
        _audit = audit;
    }

    public Task<EmailTemplate?> GetAsync(Guid id, CancellationToken ct = default) => _repo.GetByIdAsync(id, ct);

    public Task<EmailTemplate?> GetByKeyAsync(string key, CancellationToken ct = default) => _repo.GetByKeyAsync(key, ct);

    public Task<List<EmailTemplate>> ListAsync(CancellationToken ct = default) => _repo.ListAsync(ct);

    public async Task<EmailTemplate> UpdateAsync(Guid id, string subject, string htmlBody, string? plainTextBody, CancellationToken ct = default)
    {
        var t = await _repo.GetByIdAsync(id, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"EmailTemplate {id} not found.");
        t.Subject = subject;
        t.HtmlBody = htmlBody;
        t.PlainTextBody = plainTextBody;
        t.ModifiedAt = DateTimeOffset.UtcNow;
        await _repo.UpdateAsync(t, ct).ConfigureAwait(false);
        await _audit.WriteAsync(
            "EmailTemplate.Updated",
            nameof(EmailTemplate),
            t.Id.ToString(),
            details: new { t.TemplateKey, t.Subject },
            cancellationToken: ct).ConfigureAwait(false);
        return t;
    }
}
