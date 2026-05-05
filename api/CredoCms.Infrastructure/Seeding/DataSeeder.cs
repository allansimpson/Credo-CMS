using CredoCms.Domain.Announcements;
using CredoCms.Domain.Common;
using CredoCms.Domain.Identity;
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
}
