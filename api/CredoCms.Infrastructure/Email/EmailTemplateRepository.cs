using CredoCms.Application.Email;
using CredoCms.Domain.Email;
using CredoCms.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CredoCms.Infrastructure.Email;

public sealed class EmailTemplateRepository : IEmailTemplateRepository
{
    private readonly ApplicationDbContext _db;
    public EmailTemplateRepository(ApplicationDbContext db) => _db = db;

    public Task<EmailTemplate?> GetByKeyAsync(string templateKey, CancellationToken ct = default) =>
        _db.EmailTemplates.AsNoTracking().FirstOrDefaultAsync(t => t.TemplateKey == templateKey, ct);

    public Task<EmailTemplate?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.EmailTemplates.FirstOrDefaultAsync(t => t.Id == id, ct);

    public Task<List<EmailTemplate>> ListAsync(CancellationToken ct = default) =>
        _db.EmailTemplates.AsNoTracking().OrderBy(t => t.TemplateKey).ToListAsync(ct);

    public async Task AddAsync(EmailTemplate template, CancellationToken ct = default)
    {
        _db.EmailTemplates.Add(template);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task UpdateAsync(EmailTemplate template, CancellationToken ct = default)
    {
        _db.EmailTemplates.Update(template);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
