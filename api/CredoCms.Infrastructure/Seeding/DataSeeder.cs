using CredoCms.Domain.Announcements;
using CredoCms.Domain.Blog;
using CredoCms.Domain.Common;
using CredoCms.Domain.Email;
using CredoCms.Domain.Events;
using CredoCms.Domain.Groups;
using CredoCms.Domain.Identity;
using CredoCms.Domain.Leaders;
using CredoCms.Domain.News;
using CredoCms.Domain.Pages;
using CredoCms.Domain.Sermons;
using CredoCms.Domain.Volunteers;
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
        await SeedSampleSermonContentAsync(ct).ConfigureAwait(false);
        await SeedSampleEventsAsync(ct).ConfigureAwait(false);
        await SeedSampleGroupsAsync(ct).ConfigureAwait(false);
        await SeedSampleBlogPostsAsync(ct).ConfigureAwait(false);
        // Phase 5
        await SeedEmailTemplatesAsync(ct).ConfigureAwait(false);
        await SeedSampleBroadcastAsync(ct).ConfigureAwait(false);
        await SeedSampleVolunteerRolesAsync(ct).ConfigureAwait(false);
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

    private async Task SeedSampleSermonContentAsync(CancellationToken ct)
    {
        if (await _db.SermonSeries.AnyAsync(ct).ConfigureAwait(false)) return;
        if (await _db.Sermons.AnyAsync(ct).ConfigureAwait(false)) return;

        var now = DateTimeOffset.UtcNow;

        var seriesRomans = new SermonSeries
        {
            Id = Guid.NewGuid(),
            Slug = "the-letter-to-the-romans",
            Title = "The Letter to the Romans",
            DescriptionJson = ParaJson("A walk through Paul's most theologically rich letter."),
            StartDate = DateOnly.FromDateTime(now.UtcDateTime).AddMonths(-2),
            CreatedAt = now, ModifiedAt = now,
        };
        var seriesPsalms = new SermonSeries
        {
            Id = Guid.NewGuid(),
            Slug = "psalms-of-ascent",
            Title = "Psalms of Ascent",
            DescriptionJson = ParaJson("Songs sung on the road to Jerusalem — and to God."),
            StartDate = DateOnly.FromDateTime(now.UtcDateTime).AddMonths(-1),
            CreatedAt = now, ModifiedAt = now,
        };
        _db.SermonSeries.AddRange(seriesRomans, seriesPsalms);

        // YouTube IDs are placeholders — Editors are expected to replace them.
        _db.Sermons.AddRange(
            SampleSermon("good-news-for-everyone", "Good News for Everyone (Romans 1)",
                "dQw4w9WgXcQ", seriesRomans.Id, now.AddDays(-21), "Pastor Marcus"),
            SampleSermon("the-righteousness-of-god", "The Righteousness of God (Romans 3)",
                "9bZkp7q19f0", seriesRomans.Id, now.AddDays(-14), "Pastor Marcus"),
            SampleSermon("by-faith-alone", "By Faith Alone (Romans 4)",
                "kJQP7kiw5Fk", seriesRomans.Id, now.AddDays(-7), "Pastor Marcus"),
            SampleSermon("i-lift-up-my-eyes", "I Lift Up My Eyes (Psalm 121)",
                "fLexgOxsZu0", seriesPsalms.Id, now.AddDays(-3), "Elder Sarah"));

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        _logger.LogInformation("Seeded sample sermon series + sermons.");
    }

    private static Sermon SampleSermon(string slug, string title, string ytId,
        Guid seriesId, DateTimeOffset publishedAt, string speaker)
        => new()
        {
            Id = Guid.NewGuid(),
            Slug = slug,
            Title = title,
            YouTubeVideoId = ytId,
            DescriptionJson = ParaJson($"Notes for {title}."),
            PublishedAt = publishedAt,
            YouTubePublishedAt = publishedAt,
            SpeakerNameFreeText = speaker,
            SermonSeriesId = seriesId,
            IsPublished = true,
            CreatedAt = publishedAt,
            ModifiedAt = publishedAt,
        };

    private async Task SeedSampleEventsAsync(CancellationToken ct)
    {
        if (await _db.Events.AnyAsync(ct).ConfigureAwait(false)) return;
        var now = DateTimeOffset.UtcNow;

        // Single events
        var picnic = new Event
        {
            Id = Guid.NewGuid(), Slug = "summer-picnic", Title = "Summer Picnic",
            DescriptionJson = ParaJson("Bring a side dish and lawn chairs."),
            StartsAt = now.Date.AddDays(14).AddHours(11),
            EndsAt = now.Date.AddDays(14).AddHours(15),
            Location = "Memorial Park, Pavilion 3",
            Visibility = EventVisibility.Public,
            RegistrationMode = EventRegistrationMode.RsvpOptional,
            Capacity = 80, WaitlistEnabled = true,
            IsPublished = true,
            CreatedAt = now, ModifiedAt = now,
        };
        var workshop = new Event
        {
            Id = Guid.NewGuid(), Slug = "marriage-workshop", Title = "Marriage Workshop",
            DescriptionJson = ParaJson("A Saturday workshop for couples — registration required."),
            StartsAt = now.Date.AddDays(30).AddHours(9),
            EndsAt = now.Date.AddDays(30).AddHours(15),
            Location = "Fellowship Hall",
            Visibility = EventVisibility.Public,
            RegistrationMode = EventRegistrationMode.RegistrationRequired,
            Capacity = 24, WaitlistEnabled = true,
            RegistrationOpensAt = now,
            RegistrationClosesAt = now.AddDays(28),
            IsPublished = true,
            CreatedAt = now, ModifiedAt = now,
        };

        // Recurring weekly: Wednesday Bible study
        var bibleStudy = new Event
        {
            Id = Guid.NewGuid(), Slug = "wednesday-bible-study", Title = "Wednesday Bible Study",
            DescriptionJson = ParaJson("Weekly verse-by-verse study. Childcare provided."),
            StartsAt = NextDayOfWeek(now, DayOfWeek.Wednesday).AddHours(19),
            EndsAt = NextDayOfWeek(now, DayOfWeek.Wednesday).AddHours(20).AddMinutes(30),
            Location = "Room 204",
            Visibility = EventVisibility.Public,
            RecurrenceRule = "FREQ=WEEKLY;BYDAY=WE",
            IsPublished = true,
            CreatedAt = now, ModifiedAt = now,
        };

        // Recurring monthly: members' prayer breakfast (members-only)
        var prayerBreakfast = new Event
        {
            Id = Guid.NewGuid(), Slug = "members-prayer-breakfast", Title = "Members' Prayer Breakfast",
            DescriptionJson = ParaJson("First Saturday of each month."),
            StartsAt = FirstSaturdayOfNextMonth(now).AddHours(8),
            EndsAt = FirstSaturdayOfNextMonth(now).AddHours(9).AddMinutes(30),
            Location = "Fellowship Hall",
            Visibility = EventVisibility.MembersOnly,
            RecurrenceRule = "FREQ=MONTHLY;BYMONTHDAY=1",
            IsPublished = true,
            CreatedAt = now, ModifiedAt = now,
        };

        // Single members-only with external URL
        var retreat = new Event
        {
            Id = Guid.NewGuid(), Slug = "annual-retreat", Title = "Annual Members' Retreat",
            DescriptionJson = ParaJson("A weekend away — registration via the camp's site."),
            StartsAt = now.Date.AddDays(60).AddHours(17),
            EndsAt = now.Date.AddDays(62).AddHours(15),
            Location = "Camp Cedarwood",
            Visibility = EventVisibility.MembersOnly,
            ExternalRegistrationUrl = "https://example.org/retreat",
            RegistrationMode = EventRegistrationMode.RegistrationRequired,
            IsPublished = true,
            CreatedAt = now, ModifiedAt = now,
        };

        _db.Events.AddRange(picnic, workshop, bibleStudy, prayerBreakfast, retreat);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        _logger.LogInformation("Seeded sample events (single, weekly, monthly, members-only).");
    }

    private static DateTimeOffset NextDayOfWeek(DateTimeOffset from, DayOfWeek target)
    {
        var diff = ((int)target - (int)from.DayOfWeek + 7) % 7;
        if (diff == 0) diff = 7;
        return from.Date.AddDays(diff);
    }

    private static DateTimeOffset FirstSaturdayOfNextMonth(DateTimeOffset from)
    {
        var firstOfNext = new DateTime(from.Year, from.Month, 1).AddMonths(1);
        var dow = firstOfNext.DayOfWeek;
        var diff = ((int)DayOfWeek.Saturday - (int)dow + 7) % 7;
        return new DateTimeOffset(firstOfNext.AddDays(diff), TimeSpan.Zero);
    }

    private static string ParaJson(string text)
        => "{\"type\":\"doc\",\"content\":[{\"type\":\"paragraph\",\"content\":[{\"type\":\"text\",\"text\":\""
           + text.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"}]}]}";

    // ---- Phase 4 seed methods ---------------------------------------------

    private async Task SeedSampleGroupsAsync(CancellationToken ct)
    {
        if (await _db.Groups.AnyAsync(ct).ConfigureAwait(false)) return;
        var now = DateTimeOffset.UtcNow;
        var youth = new Group
        {
            Id = Guid.NewGuid(),
            Slug = "youth-group",
            Name = "Youth Group",
            DescriptionJson = ParaJson("Sunday-evening community for middle and high school students."),
            MeetingInfo = "Sundays · 6:00 pm · Fellowship Hall",
            ContactEmail = "youth@example.org",
            Visibility = GroupVisibility.Public,
            Joinability = GroupJoinability.Open,
            RequiresMessageOnJoinRequest = MessageOnJoinRequest.Optional,
            RosterVisibility = RosterVisibility.LeadersOnly,
            IsActive = true,
            CreatedAt = now, ModifiedAt = now,
        };
        var menBibleStudy = new Group
        {
            Id = Guid.NewGuid(),
            Slug = "mens-bible-study",
            Name = "Men's Bible Study",
            DescriptionJson = ParaJson("Saturday-morning study for men. Coffee provided."),
            MeetingInfo = "Saturdays · 7:00 am · Library",
            Visibility = GroupVisibility.MembersOnly,
            Joinability = GroupJoinability.Open,
            RequiresMessageOnJoinRequest = MessageOnJoinRequest.Optional,
            RosterVisibility = RosterVisibility.AllGroupMembers,
            IsActive = true,
            CreatedAt = now, ModifiedAt = now,
        };
        _db.Groups.AddRange(youth, menBibleStudy);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        _logger.LogInformation("Seeded sample groups (youth + men's Bible study).");
    }

    private async Task SeedSampleBlogPostsAsync(CancellationToken ct)
    {
        if (await _db.BlogPosts.AnyAsync(ct).ConfigureAwait(false)) return;
        var admin = await _userManager.FindByEmailAsync(_identitySeed.DefaultAdminEmail).ConfigureAwait(false);
        if (admin is null) return;

        var now = DateTimeOffset.UtcNow;
        var welcome = new BlogPost
        {
            Id = Guid.NewGuid(),
            Slug = "welcome-to-our-blog",
            Title = "Welcome to our blog",
            BodyJson = ParaJson(
                "We're starting a regular space for devotionals, sermon notes, and reflections " +
                "from the pastors. Subscribe via the church newsletter to be notified when new " +
                "posts go up."),
            Excerpt = "A space for devotionals, sermon notes, and reflections.",
            AuthorUserId = admin.Id,
            Category = "Announcements",
            IsPublished = true,
            IsPinned = true,
            PublishedAt = now,
            ReadingTimeMinutes = 1,
            CreatedAt = now, ModifiedAt = now,
            ModifiedByUserId = admin.Id,
        };
        _db.BlogPosts.Add(welcome);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        _logger.LogInformation("Seeded sample blog post (welcome).");
    }

    // ---- Phase 5 seeds ---------------------------------------------------

    private async Task SeedEmailTemplatesAsync(CancellationToken ct)
    {
        if (await _db.EmailTemplates.AnyAsync(ct).ConfigureAwait(false)) return;

        var now = DateTimeOffset.UtcNow;
        var seeds = new (string Key, string Subject, string Html, string Description, string[] Vars)[]
        {
            ("InvitationEmail", "You're invited to {{churchName}}",
             "<p>Hi {{firstName}},</p><p>You've been invited to join {{churchName}}'s online community.</p><p><a href=\"{{invitationLink}}\">Accept your invitation</a></p>",
             "Sent when an admin creates a user account.",
             new[] { "firstName", "lastName", "invitationLink" }),

            ("PasswordReset", "Reset your password — {{churchName}}",
             "<p>Hi {{firstName}},</p><p>A password reset was requested. <a href=\"{{resetLink}}\">Choose a new password</a>.</p><p>If you didn't request this, no action is needed.</p>",
             "Sent when a user requests a password reset.",
             new[] { "firstName", "resetLink" }),

            ("AccountActivated", "Welcome to {{churchName}}",
             "<p>Hi {{firstName}},</p><p>Your account is active. You can now sign in to {{churchName}}.</p>",
             "Sent when a user accepts an invitation.",
             new[] { "firstName" }),

            ("ConnectCardAcknowledgment", "Thanks for connecting with {{churchName}}",
             "<p>Hi {{firstName}},</p><p>Thanks for filling out our connect card. We'll be in touch soon.</p>",
             "Sent to connect-card submitters.",
             new[] { "firstName" }),

            ("GroupJoinApproved", "You're in: {{groupName}}",
             "<p>Hi {{firstName}},</p><p>Your request to join <strong>{{groupName}}</strong> was approved. Welcome!</p>",
             "Sent when a group join request is approved.",
             new[] { "firstName", "groupName" }),

            ("GroupJoinDeclined", "About your request to join {{groupName}}",
             "<p>Hi {{firstName}},</p><p>Your request to join {{groupName}} was not approved at this time.</p>",
             "Sent when a group join request is declined.",
             new[] { "firstName", "groupName" }),

            ("EventRegistrationConfirmation", "Registered for {{eventTitle}}",
             "<p>Hi {{firstName}},</p><p>You're registered for <strong>{{eventTitle}}</strong> on {{eventDate}}.</p>",
             "Sent on event registration.",
             new[] { "firstName", "eventTitle", "eventDate" }),

            ("EventRegistrationCancellation", "Cancellation: {{eventTitle}}",
             "<p>Hi {{firstName}},</p><p>Your registration for {{eventTitle}} has been canceled.</p>",
             "Sent when an event registration is canceled.",
             new[] { "firstName", "eventTitle" }),

            ("EventRegistrationWaitlistPromotion", "You're off the waitlist: {{eventTitle}}",
             "<p>Hi {{firstName}},</p><p>A spot opened up for <strong>{{eventTitle}}</strong> and you're now confirmed.</p>",
             "Sent when a waitlisted entry is promoted.",
             new[] { "firstName", "eventTitle" }),

            ("EventRegistrationReminder", "Reminder: {{eventTitle}} on {{eventDate}}",
             "<p>Hi {{firstName}},</p><p>This is a reminder that you're registered for <strong>{{eventTitle}}</strong> on {{eventDate}}.</p>",
             "Sent 24-48h before an event.",
             new[] { "firstName", "eventTitle", "eventDate" }),

            ("BroadcastUnsubscribeConfirmation", "You've unsubscribed from {{churchName}} emails",
             "<p>Hi {{firstName}},</p><p>You've been unsubscribed from {{categoryLabel}} emails. You can re-enable preferences any time at <a href=\"{{preferencesLink}}\">your account</a>.</p>",
             "Sent after a one-click unsubscribe.",
             new[] { "firstName", "categoryLabel", "preferencesLink" }),

            ("EventVolunteerSignupConfirmation", "Thanks for volunteering at {{eventTitle}}",
             "<p>Hi {{firstName}},</p><p>You're signed up to volunteer as <strong>{{roleName}}</strong> for {{eventTitle}} on {{occurrenceDate}}.</p>",
             "Sent on volunteer signup.",
             new[] { "firstName", "roleName", "eventTitle", "occurrenceDate" }),

            ("EventVolunteerCancellation", "Volunteer slot canceled",
             "<p>Hi {{firstName}},</p><p>Your volunteer commitment for {{eventTitle}} on {{occurrenceDate}} has been canceled.</p>",
             "Sent when a volunteer signup is canceled.",
             new[] { "firstName", "eventTitle", "occurrenceDate" }),

            ("EventVolunteerReminder", "Volunteer reminder: {{roleName}} on {{occurrenceDate}}",
             "<p>Hi {{firstName}},</p><p>You're scheduled to serve as <strong>{{roleName}}</strong> at {{eventTitle}} on {{occurrenceDate}}.</p>",
             "24-48h volunteer reminder.",
             new[] { "firstName", "roleName", "eventTitle", "occurrenceDate" }),

            ("ConnectCardDigest", "{{count}} new connect card{{plural}}",
             "<p>Hi {{firstName}},</p><p>You have {{count}} new connect card submission{{plural}} to review.</p><p><a href=\"/admin/connect-cards\">View in admin</a></p>",
             "Admin digest for new connect cards.",
             new[] { "firstName", "count", "plural" }),

            ("GroupJoinRequestDigest", "{{count}} new group join request{{plural}}",
             "<p>Hi {{firstName}},</p><p>You have {{count}} new group join request{{plural}} to review.</p><p><a href=\"/admin/groups\">View in admin</a></p>",
             "Admin digest for new group join requests.",
             new[] { "firstName", "count", "plural" }),
        };

        foreach (var (key, subject, html, desc, vars) in seeds)
        {
            _db.EmailTemplates.Add(new EmailTemplate
            {
                Id = Guid.NewGuid(),
                TemplateKey = key,
                Subject = subject,
                HtmlBody = html,
                AvailableMergeFieldsJson = System.Text.Json.JsonSerializer.Serialize(vars),
                IsSystemTemplate = true,
                Description = desc,
                CreatedAt = now,
                ModifiedAt = now,
            });
        }
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        _logger.LogInformation("Seeded {Count} system email templates.", seeds.Length);
    }

    private async Task SeedSampleBroadcastAsync(CancellationToken ct)
    {
        if (await _db.EmailBroadcasts.AnyAsync(ct).ConfigureAwait(false)) return;
        var admin = await _userManager.FindByEmailAsync(_identitySeed.DefaultAdminEmail).ConfigureAwait(false);
        if (admin is null) return;

        var sentAt = DateTimeOffset.UtcNow.AddDays(-3);
        var sample = new EmailBroadcast
        {
            Id = Guid.NewGuid(),
            Subject = "Welcome to your church communications",
            Body = "<p>Welcome! You're now subscribed to broadcasts from our church. We'll send a few updates a month.</p>",
            PlainTextBody = "Welcome! You're now subscribed to broadcasts from our church.",
            TargetMode = BroadcastTargetMode.AllMembers,
            SendMode = BroadcastSendMode.SendNow,
            Status = BroadcastStatus.Sent,
            Category = EmailCategory.Broadcast,
            SentAt = sentAt,
            RecipientCountAtSend = 3,
            DeliveredCount = 3,
            CreatedAt = sentAt,
            ModifiedAt = sentAt,
            ModifiedByUserId = admin.Id,
        };
        _db.EmailBroadcasts.Add(sample);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        _logger.LogInformation("Seeded sample broadcast.");
    }

    private async Task SeedSampleVolunteerRolesAsync(CancellationToken ct)
    {
        if (await _db.EventVolunteerRoles.AnyAsync(ct).ConfigureAwait(false)) return;
        // Pick the first seeded event (small group / Sunday gathering) and
        // attach a couple of roles to it.
        var firstEvent = await _db.Events.AsNoTracking().OrderBy(e => e.StartsAt).FirstOrDefaultAsync(ct).ConfigureAwait(false);
        if (firstEvent is null) return;

        var now = DateTimeOffset.UtcNow;
        _db.EventVolunteerRoles.AddRange(
            new EventVolunteerRole
            {
                Id = Guid.NewGuid(),
                EventId = firstEvent.Id,
                RoleName = "Setup Crew",
                Description = "Arrive 30 minutes early to set up chairs, tables, and refreshments.",
                SlotsNeeded = 2,
                DisplayOrder = 0,
                CreatedAt = now, ModifiedAt = now,
            },
            new EventVolunteerRole
            {
                Id = Guid.NewGuid(),
                EventId = firstEvent.Id,
                RoleName = "Greeter",
                Description = "Welcome attendees, hand out nametags.",
                SlotsNeeded = 2,
                DisplayOrder = 1,
                CreatedAt = now, ModifiedAt = now,
            });
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        _logger.LogInformation("Seeded sample volunteer roles on event {Id}.", firstEvent.Id);
    }
}
