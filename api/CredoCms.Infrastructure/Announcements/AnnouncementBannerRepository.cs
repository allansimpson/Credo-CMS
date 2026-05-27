using CredoCms.Application.Announcements;
using CredoCms.Domain.Announcements;
using CredoCms.Domain.Common;
using CredoCms.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CredoCms.Infrastructure.Announcements;

public sealed class AnnouncementBannerRepository : IAnnouncementBannerRepository
{
    private readonly ApplicationDbContext _db;
    public AnnouncementBannerRepository(ApplicationDbContext db) => _db = db;

    public async Task<AnnouncementBanner> GetAsync(CancellationToken ct = default)
    {
        var b = await _db.AnnouncementBanner
            .FirstOrDefaultAsync(x => x.Id == SystemConstants.AnnouncementBannerId, ct)
            .ConfigureAwait(false);
        if (b is not null) return b;

        // Lazy-create the singleton if seeding hasn't run (graceful for
        // first-boot dev environments).
        var now = DateTimeOffset.UtcNow;
        b = new AnnouncementBanner
        {
            Id = SystemConstants.AnnouncementBannerId,
            IsActive = false,
            Severity = AnnouncementSeverity.Info,
            Message = "",
            CreatedAt = now,
            ModifiedAt = now,
        };
        _db.AnnouncementBanner.Add(b);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        return b;
    }

    public async Task UpdateAsync(AnnouncementBanner banner, CancellationToken ct = default)
    {
        _db.AnnouncementBanner.Update(banner);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
