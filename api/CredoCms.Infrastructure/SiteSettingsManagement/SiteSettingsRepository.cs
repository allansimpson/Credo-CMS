using CredoCms.Application.SiteSettingsManagement;
using CredoCms.Domain.Common;
using CredoCms.Domain.Settings;
using CredoCms.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CredoCms.Infrastructure.SiteSettingsManagement;

public sealed class SiteSettingsRepository : ISiteSettingsRepository
{
    private readonly ApplicationDbContext _db;

    public SiteSettingsRepository(ApplicationDbContext db) => _db = db;

    public async Task<SiteSettings> GetAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _db.SiteSettings
            .FirstOrDefaultAsync(s => s.Id == SystemConstants.SiteSettingsId, cancellationToken)
            .ConfigureAwait(false);

        return settings ?? throw new InvalidOperationException(
            "SiteSettings has not been seeded. Apply migrations and ensure the seeder ran.");
    }

    public async Task UpdateAsync(SiteSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);

        try
        {
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new OptimisticConcurrencyException(
                "SiteSettings was modified by another user. Reload and try again.", ex);
        }
    }
}
