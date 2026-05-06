using CredoCms.Domain.Email;

namespace CredoCms.Application.Email;

public interface IEmailTemplateRepository
{
    Task<EmailTemplate?> GetByKeyAsync(string templateKey, CancellationToken ct = default);
    Task<EmailTemplate?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<EmailTemplate>> ListAsync(CancellationToken ct = default);
    Task AddAsync(EmailTemplate template, CancellationToken ct = default);
    Task UpdateAsync(EmailTemplate template, CancellationToken ct = default);
}
