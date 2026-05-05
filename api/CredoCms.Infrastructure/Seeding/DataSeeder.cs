using CredoCms.Domain.Announcements;
using CredoCms.Domain.Common;
using CredoCms.Domain.Identity;
using CredoCms.Domain.Leaders;
using CredoCms.Domain.News;
using CredoCms.Domain.Pages;
using CredoCms.Domain.Services;
using CredoCms.Domain.Settings;
using CredoCms.Infrastructure.Configuration;
using CredoCms.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CredoCms.Infrastructure.Seeding;

/// <summary>
/// Idempotent first-run seeder. Runs on application startup; if the database is
/// empty it creates the three roles, the default Administrator, the System User,
/// and the single Site Settings row. Subsequent runs do nothing.
/// </summary>
public sealed class DataSeeder
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly IdentitySeedOptions _identitySeed;
    private readonly ILogger<DataSeeder> _logger;

    public DataSeeder(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        IOptions<IdentitySeedOptions> identitySeed,
        ILogger<DataSeeder> logger)
    {
        _db = db;
        _userManager = userManager;
        _roleManager = roleManager;
        _identitySeed = identitySeed.Value;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        await SeedRolesAsync(ct).ConfigureAwait(false);
        await SeedSystemUserAsync(ct).ConfigureAwait(false);
        await SeedDefaultAdminAsync(ct).ConfigureAwait(false);
        await SeedSiteSettingsAsync(ct).ConfigureAwait(false);
        await SeedAnnouncementBannerAsync(ct).ConfigureAwait(false);
        await SeedSystemPagesAsync(ct).ConfigureAwait(false);
        await SeedSamplePagesAsync(ct).ConfigureAwait(false);
        await SeedSampleServiceTimesAsync(ct).ConfigureAwait(false);
        await SeedSampleLeadersAsync(ct).ConfigureAwait(false);
        await SeedSampleNewsAsync(ct).ConfigureAwait(false);
    }

    private async Task SeedRolesAsync(CancellationToken ct)
    {
        foreach (var roleName in SystemConstants.Roles.All)
        {
            if (!await _roleManager.RoleExistsAsync(roleName).ConfigureAwait(false))
            {
                var result = await _roleManager.CreateAsync(new ApplicationRole(roleName)).ConfigureAwait(false);
                if (result.Succeeded)
                {
                    _logger.LogInformation("Seeded role {Role}", roleName);
                }
                else
                {
                    _logger.LogError("Failed to seed role {Role}: {Errors}", roleName,
                        string.Join("; ", result.Errors.Select(e => e.Description)));
                }
            }
        }
    }

    private async Task SeedSystemUserAsync(CancellationToken ct)
    {
        var existing = await _userManager.FindByIdAsync(SystemConstants.SystemUserId.ToString()).ConfigureAwait(false);
        if (existing is not null) return;

        var systemUser = new ApplicationUser
        {
            Id = SystemConstants.SystemUserId,
            UserName = SystemConstants.SystemUserEmail,
            Email = SystemConstants.SystemUserEmail,
            EmailConfirmed = true,
            FirstName = "System",
            LastName = "User",
            IsActive = false,            // not loginable
            CreatedAt = DateTimeOffset.UtcNow,
            LockoutEnabled = false,
            SecurityStamp = Guid.NewGuid().ToString("N"),
        };

        // Create the system user without a password — UserManager.CreateAsync(user)
        // (no password overload) creates an account that cannot sign in.
        var result = await _userManager.CreateAsync(systemUser).ConfigureAwait(false);
        if (!result.Succeeded)
        {
            _logger.LogError("Failed to seed System User: {Errors}",
                string.Join("; ", result.Errors.Select(e => e.Description)));
        }
        else
        {
            _logger.LogInformation("Seeded System User with id {Id}", systemUser.Id);
        }
    }

    private async Task SeedDefaultAdminAsync(CancellationToken ct)
    {
        var existing = await _userManager.FindByEmailAsync(_identitySeed.DefaultAdminEmail).ConfigureAwait(false);
        if (existing is not null) return;

        var admin = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = _identitySeed.DefaultAdminEmail,
            Email = _identitySeed.DefaultAdminEmail,
            EmailConfirmed = true,
            FirstName = "Default",
            LastName = "Administrator",
            IsActive = true,
            RequirePasswordChangeOnFirstLogin = true,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        var createResult = await _userManager.CreateAsync(admin, _identitySeed.DefaultAdminPassword).ConfigureAwait(false);
        if (!createResult.Succeeded)
        {
            _logger.LogError("Failed to seed default Administrator: {Errors}",
                string.Join("; ", createResult.Errors.Select(e => e.Description)));
            return;
        }

        var roleResult = await _userManager.AddToRoleAsync(admin, SystemConstants.Roles.Administrator).ConfigureAwait(false);
        if (!roleResult.Succeeded)
        {
            _logger.LogError("Seeded admin user but failed to assign Administrator role: {Errors}",
                string.Join("; ", roleResult.Errors.Select(e => e.Description)));
        }
        else
        {
            _logger.LogWarning(
                "Seeded default Administrator {Email}. RequirePasswordChangeOnFirstLogin is set; "
                + "the operator MUST change this password immediately after first sign-in.",
                _identitySeed.DefaultAdminEmail);
        }
    }

    private async Task SeedSiteSettingsAsync(CancellationToken ct)
    {
        var exists = await _db.SiteSettings.AnyAsync(s => s.Id == SystemConstants.SiteSettingsId, ct).ConfigureAwait(false);
        if (exists) return;

        var now = DateTimeOffset.UtcNow;
        _db.SiteSettings.Add(new SiteSettings
        {
            Id = SystemConstants.SiteSettingsId,
            ChurchName = "Your Church Name",
            Tagline = "Welcome to our community",
            PrimaryColor = "#1e3a8a",
            AccentColor = "#f59e0b",
            DefaultVersionRetentionCount = 20,
            CreatedAt = now,
            ModifiedAt = now,
            ModifiedByUserId = SystemConstants.SystemUserId,
            RowVersion = [],
        });

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        _logger.LogInformation("Seeded SiteSettings row");
    }

    private async Task SeedAnnouncementBannerAsync(CancellationToken ct)
    {
        var exists = await _db.AnnouncementBanner.AnyAsync(
            b => b.Id == SystemConstants.AnnouncementBannerId, ct).ConfigureAwait(false);
        if (exists) return;

        var now = DateTimeOffset.UtcNow;
        _db.AnnouncementBanner.Add(new AnnouncementBanner
        {
            Id = SystemConstants.AnnouncementBannerId,
            IsActive = false,
            Severity = AnnouncementSeverity.Info,
            Message = "",
            CreatedAt = now,
            ModifiedAt = now,
            ModifiedByUserId = SystemConstants.SystemUserId,
        });

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        _logger.LogInformation("Seeded inactive AnnouncementBanner singleton");
    }

    private async Task SeedSystemPagesAsync(CancellationToken ct)
    {
        if (await _db.Pages.AnyAsync(p => p.IsSystemPage, ct).ConfigureAwait(false)) return;
        var now = DateTimeOffset.UtcNow;
        // Slugs match the Phase 2 prompt's "/privacy-policy" and
        // "/terms-of-service" public routes and the footer links in the SPA.
        _db.Pages.AddRange(
            SamplePage("privacy-policy", "Privacy Policy", isSystem: true, now: now,
                paragraph: "This is a placeholder Privacy Policy. Please replace with the policy that governs your church's data practices."),
            SamplePage("terms-of-service", "Terms of Service", isSystem: true, now: now,
                paragraph: "Placeholder Terms of Service. Replace with the terms that apply to your site."));
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        _logger.LogInformation("Seeded system pages (Privacy, Terms).");
    }

    private async Task SeedSamplePagesAsync(CancellationToken ct)
    {
        if (await _db.Pages.AnyAsync(p => p.Slug == "about" && !p.IsSystemPage, ct).ConfigureAwait(false))
            return;
        var now = DateTimeOffset.UtcNow;
        _db.Pages.AddRange(
            SamplePage("about", "About Us", now: now,
                paragraph: "We are a community of believers committed to following Jesus together. Replace this text with your church's story."),
            SamplePage("plan-your-visit", "Plan Your Visit", now: now,
                paragraph: "Visiting for the first time? Here's what to expect when you join us on Sunday."),
            SamplePage("what-we-believe", "What We Believe", now: now,
                paragraph: "Our beliefs in summary. Replace with your statement of faith."));
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        _logger.LogInformation("Seeded sample pages (About, Plan Your Visit, What We Believe).");
    }

    private async Task SeedSampleServiceTimesAsync(CancellationToken ct)
    {
        if (await _db.ServiceTimes.AnyAsync(ct).ConfigureAwait(false)) return;
        var now = DateTimeOffset.UtcNow;
        _db.ServiceTimes.AddRange(
            new ServiceTime { Id = Guid.NewGuid(), Name = "Sunday Worship", DayOfWeek = DayOfWeek.Sunday,
                StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(10, 30),
                Location = "Main Sanctuary", DisplayOrder = 0, IsActive = true,
                CreatedAt = now, ModifiedAt = now },
            new ServiceTime { Id = Guid.NewGuid(), Name = "Sunday School", DayOfWeek = DayOfWeek.Sunday,
                StartTime = new TimeOnly(11, 0), EndTime = new TimeOnly(12, 0),
                Location = "Education Wing", DisplayOrder = 1, IsActive = true,
                CreatedAt = now, ModifiedAt = now },
            new ServiceTime { Id = Guid.NewGuid(), Name = "Wednesday Bible Study", DayOfWeek = DayOfWeek.Wednesday,
                StartTime = new TimeOnly(19, 0), EndTime = new TimeOnly(20, 30),
                Location = "Fellowship Hall", DisplayOrder = 0, IsActive = true,
                CreatedAt = now, ModifiedAt = now });
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        _logger.LogInformation("Seeded sample service times.");
    }

    private async Task SeedSampleLeadersAsync(CancellationToken ct)
    {
        if (await _db.Leaders.AnyAsync(ct).ConfigureAwait(false)) return;
        var now = DateTimeOffset.UtcNow;
        _db.Leaders.AddRange(
            new Leader { Id = Guid.NewGuid(), FullName = "Lead Pastor",
                Title = "Senior Pastor", Category = "Pastoral Staff", DisplayOrder = 0,
                CreatedAt = now, ModifiedAt = now },
            new Leader { Id = Guid.NewGuid(), FullName = "Associate Pastor",
                Title = "Family Ministry", Category = "Pastoral Staff", DisplayOrder = 1,
                CreatedAt = now, ModifiedAt = now },
            new Leader { Id = Guid.NewGuid(), FullName = "Elder One",
                Title = null, Category = "Elders", DisplayOrder = 0,
                CreatedAt = now, ModifiedAt = now },
            new Leader { Id = Guid.NewGuid(), FullName = "Deacon One",
                Title = null, Category = "Deacons", DisplayOrder = 0,
                CreatedAt = now, ModifiedAt = now });
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        _logger.LogInformation("Seeded sample leaders (placeholder names — replace with real staff).");
    }

    private async Task SeedSampleNewsAsync(CancellationToken ct)
    {
        if (await _db.News.AnyAsync(ct).ConfigureAwait(false)) return;
        var now = DateTimeOffset.UtcNow;
        _db.News.AddRange(
            SampleNews("welcome-to-our-new-site", "Welcome to our new site",
                "We've launched a new website! Take a look around.", isMembersOnly: false, now: now),
            SampleNews("upcoming-summer-camp", "Summer Camp Registration Open",
                "Members can now register for this year's summer camp.", isMembersOnly: true, now: now));
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        _logger.LogInformation("Seeded sample news items.");
    }

    private static Page SamplePage(string slug, string title, DateTimeOffset now,
        string paragraph, bool isSystem = false)
    {
        var body = "{\"type\":\"doc\",\"content\":[{\"type\":\"paragraph\",\"content\":[{\"type\":\"text\",\"text\":\""
            + paragraph.Replace("\"", "\\\"") + "\"}]}]}";
        return new Page
        {
            Id = Guid.NewGuid(),
            Slug = slug, Title = title, BodyJson = body, Excerpt = paragraph,
            IsPublished = true, IsMembersOnly = false, IsSystemPage = isSystem,
            CreatedAt = now, ModifiedAt = now, PublishedAt = now,
        };
    }

    private static NewsItem SampleNews(string slug, string title, string paragraph,
        bool isMembersOnly, DateTimeOffset now)
    {
        var body = "{\"type\":\"doc\",\"content\":[{\"type\":\"paragraph\",\"content\":[{\"type\":\"text\",\"text\":\""
            + paragraph.Replace("\"", "\\\"") + "\"}]}]}";
        return new NewsItem
        {
            Id = Guid.NewGuid(),
            Slug = slug, Title = title, BodyJson = body, Excerpt = paragraph,
            IsPublished = true, IsMembersOnly = isMembersOnly,
            CreatedAt = now, ModifiedAt = now, PublishedAt = now,
        };
    }
}
