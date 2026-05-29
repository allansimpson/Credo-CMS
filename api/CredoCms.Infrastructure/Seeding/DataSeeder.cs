using System.Text.Json;
using CredoCms.Domain.Announcements;
using CredoCms.Domain.Bible;
using CredoCms.Domain.Scripture;
using CredoCms.Domain.Blog;
using CredoCms.Domain.Classes;
using CredoCms.Domain.Common;
using CredoCms.Domain.ConnectCard;
using CredoCms.Domain.Documents;
using CredoCms.Domain.Email;
using CredoCms.Domain.Events;
using CredoCms.Domain.Groups;
using CredoCms.Domain.Identity;
using CredoCms.Domain.Leaders;
using CredoCms.Domain.News;
using CredoCms.Domain.Pages;
using CredoCms.Domain.Prayer;
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
        await SeedEmailTemplatesAsync(ct).ConfigureAwait(false);
        await SeedSampleBroadcastAsync(ct).ConfigureAwait(false);
        await SeedSampleVolunteerRolesAsync(ct).ConfigureAwait(false);
        await SeedSampleClassesAsync(ct).ConfigureAwait(false);
        await SeedSampleDocumentsAsync(ct).ConfigureAwait(false);
        await SeedSampleDirectoryMembersAsync(ct).ConfigureAwait(false);
        await SeedSamplePrayerRequestsAsync(ct).ConfigureAwait(false);
        await SeedSampleConnectCardsAsync(ct).ConfigureAwait(false);
    }

    // ── Infrastructure seeds ─────────────────────────────────────────────────

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

    // ── Site configuration ───────────────────────────────────────────────────

    private async Task SeedSiteSettingsAsync(CancellationToken ct)
    {
        var exists = await _db.SiteSettings.AnyAsync(s => s.Id == SystemConstants.SiteSettingsId, ct).ConfigureAwait(false);
        if (exists) return;

        var now = DateTimeOffset.UtcNow;
        _db.SiteSettings.Add(new SiteSettings
        {
            Id = SystemConstants.SiteSettingsId,
            ChurchName = "Hope Community Church",
            Tagline = "A church for people who never thought they’d be in one.",
            ContactAddress = "412 Maple Avenue, Cedar Falls, IA 50613",
            ContactPhone = "(319) 555-0184",
            ContactEmail = "office@hopecommunity.church",
            PrimaryColor = "#b8531a",
            AccentColor = "#b8531a",
            FooterText = "A community of ordinary people learning to follow Jesus together. Cedar Falls, Iowa, since 1894.",
            DefaultVersionRetentionCount = 20,
            HomepageHeroCtaLabel = "Plan a visit",
            HomepageHeroCtaLink = "/im-new",
            Template = PublicTemplate.Editorial,
            LeaderCategoriesJson = JsonSerializer.Serialize(new[] { "Ministers", "Staff", "Elders", "Deacons" }),
            BlogCategoriesJson = JsonSerializer.Serialize(new[] { "Announcements", "Devotionals", "Stories" }),
            MembersWelcomeText = Doc(
                P("Welcome back. Here’s what’s happening in your community this week.")),
            CreatedAt = now,
            ModifiedAt = now,
            ModifiedByUserId = SystemConstants.SystemUserId,
            RowVersion = [],
        });

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        _logger.LogInformation("Seeded SiteSettings row (Hope Community Church demo)");
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
            IsActive = true,
            Severity = AnnouncementSeverity.Info,
            Message = "Communion Sunday — services at 9:00 & 11:00 AM",
            LinkUrl = "/im-new",
            LinkLabel = "Plan your visit",
            CreatedAt = now,
            ModifiedAt = now,
            ModifiedByUserId = SystemConstants.SystemUserId,
        });

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        _logger.LogInformation("Seeded active AnnouncementBanner (Communion Sunday)");
    }

    // ── Pages ────────────────────────────────────────────────────────────────

    private async Task SeedSystemPagesAsync(CancellationToken ct)
    {
        if (await _db.Pages.AnyAsync(p => p.IsSystemPage, ct).ConfigureAwait(false)) return;
        var now = DateTimeOffset.UtcNow;
        _db.Pages.AddRange(
            SimplePage("privacy-policy", "Privacy Policy", isSystem: true, now: now,
                paragraph: "This is a placeholder Privacy Policy. Please replace with the policy that governs your church’s data practices."),
            SimplePage("terms-of-service", "Terms of Service", isSystem: true, now: now,
                paragraph: "Placeholder Terms of Service. Replace with the terms that apply to your site."));
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        _logger.LogInformation("Seeded system pages (Privacy, Terms).");
    }

    private async Task SeedSamplePagesAsync(CancellationToken ct)
    {
        if (await _db.Pages.AnyAsync(p => p.Slug == "about" && !p.IsSystemPage, ct).ConfigureAwait(false))
            return;
        var now = DateTimeOffset.UtcNow;

        // ── About ────────────────────────────────────────────────────────
        var aboutBody = Doc(
            P("We’re not a movement, a brand, or a destination. We’re the church that’s been on this corner since 1894 — through harvests, depressions, two world wars, and a hundred small Sundays. We expect to be here next Sunday too. You’re invited."),
            H2("A short history"),
            P("In the spring of 1894, a handful of Norwegian and German farming families pooled their savings to buy two acres at the edge of what was then a town of three hundred. They built a one-room frame chapel in a single summer, painted it white, and called it Hope."),
            P("By 1922 they had outgrown it twice. The current sanctuary — limestone foundations, oak pews, the rose window — was finished in 1924 and dedicated on the feast of St. Andrew. Most of those pews are still there."),
            P("Through the Depression the church kept a soup line in the basement five days a week. During the second war, fourteen of our members did not come home; their names are carved into the back wall."),
            P("The last fifty years have been quieter — fewer farmers, more teachers and nurses, two waves of new neighbours from Mexico and Sudan, a slow growing-up into the kind of church that holds doors instead of guarding them. We’re still that, mostly. We’re still trying."));

        // ── I'm New ──────────────────────────────────────────────────────
        var imNewBody = Doc(
            P("Visiting a new church is a strange thing. Here is everything we’d want to know if it were us — what to wear, where to park, what to do with the kids, and how long it lasts."),
            H2("The visit, hour by hour"),
            H3("8:30 — Coffee & doughnuts in the lobby"),
            P("Park anywhere. Walk in the front doors. A greeter will hand you a bulletin and point you at the coffee."),
            H3("8:50 — Kids check-in opens"),
            P("Right wing. We take a quick photo of you and your child for safety. They’ll have a name tag and you’ll have a matching pickup tag."),
            H3("9:00 — Traditional service begins"),
            P("Hymns, scripture, sermon, prayer, communion every other week. About 70 minutes. Sit anywhere — back row is fine."),
            H3("10:15 — Mid-morning fellowship"),
            P("More coffee. A few greeters in maroon name tags will introduce themselves. No pressure to stick around."),
            H3("11:00 — Contemporary service"),
            P("Same shape, different music. A band. The same sermon. Communion every other week, opposite the 9am."),
            H3("12:15 — Newcomers lunch (optional)"),
            P("First Sunday of every month. A simple meal in the fellowship hall, on us. A great way to meet the pastors."),
            H2("Things people ask us"),
            P(Bold("What should I wear? "), Txt("Whatever you wore on Saturday. Most people land somewhere between jeans and slacks.")),
            P(Bold("Will I be asked to give money? "), Txt("There’s an offering, but if you’re visiting we genuinely don’t expect anything from you. Just be here.")),
            P(Bold("How long is the service? "), Txt("About 70 minutes. We try to start and end on time.")),
            P(Bold("Where do I park? "), Txt("Anywhere in the lot off Maple. Front-row spaces are reserved for visitors and accessibility.")),
            P(Bold("I have small children — what do I do? "), Txt("Drop-off for ages 0–12 is in the right wing, opens at 8:50. Or keep them with you; we love a noisy sanctuary.")),
            P(Bold("Is the building accessible? "), Txt("Yes — a ramped main entrance, an elevator, accessible restrooms, and a hearing loop in the sanctuary.")));

        // ── What We Believe ──────────────────────────────────────────────
        var beliefsBody = Doc(
            P("We aren’t reinventing anything. What follows is a short summary of the historic Christian faith — written for normal people, not theologians."),
            H2("God"),
            P("One God, eternally existing in three persons — Father, Son, and Holy Spirit — perfect in love, holiness, and faithfulness."),
            H2("The Scriptures"),
            P("The Old and New Testaments are the inspired and authoritative Word of God, sufficient for faith and practice."),
            H2("Jesus Christ"),
            P("Truly God and truly human; born of the virgin Mary; crucified, buried, raised on the third day, and ascended to the right hand of the Father."),
            H2("Salvation"),
            P("Salvation is by grace alone, through faith alone, in Christ alone — a gift of God, not the result of our works, given for the joy of those who receive it."),
            H2("The Holy Spirit"),
            P("The Holy Spirit indwells every believer, conforming us to Christ, gifting us for service, and uniting us to the Church."),
            H2("The Church"),
            P("The Church is the family of God, gathered locally to worship, to teach, to break bread, and to live as a foretaste of the new creation."),
            H2("The Last Things"),
            P("Christ will return bodily to judge the living and the dead, and his kingdom will have no end. We wait in hope."),
            H2("The Historic Creeds"),
            P("We pray the Apostles’ Creed weekly and the Nicene Creed at communion. They aren’t ours; they’re the Church’s. We’re glad to belong to a faith that’s older than us."));

        _db.Pages.AddRange(
            new Page
            {
                Id = Guid.NewGuid(), Slug = "about", Title = "About Us",
                BodyJson = aboutBody,
                Excerpt = "One hundred and thirty years of stubborn, ordinary hope.",
                Template = PageTemplate.About,
                IsPublished = true, CreatedAt = now, ModifiedAt = now, PublishedAt = now,
            },
            new Page
            {
                Id = Guid.NewGuid(), Slug = "im-new", Title = "I’m New",
                BodyJson = imNewBody,
                Excerpt = "We saved you a seat. Here’s everything you need to know for your first visit.",
                Template = PageTemplate.ImNew,
                IsPublished = true, CreatedAt = now, ModifiedAt = now, PublishedAt = now,
            },
            new Page
            {
                Id = Guid.NewGuid(), Slug = "what-we-believe", Title = "What We Believe",
                BodyJson = beliefsBody,
                Excerpt = "An old faith, plainly said.",
                Template = PageTemplate.Beliefs,
                IsPublished = true, CreatedAt = now, ModifiedAt = now, PublishedAt = now,
            },
            new Page
            {
                Id = Guid.NewGuid(), Slug = "contact", Title = "Contact",
                BodyJson = Doc(
                    P("Have a question, a prayer request, or just want to say hello? We’d love to hear from you."),
                    P("The church office is open Monday through Thursday, 9:00 AM to 4:00 PM, and Friday mornings until noon. You can also reach us by phone or email anytime.")),
                Excerpt = "Drop us a line.",
                Template = PageTemplate.Contact,
                IsPublished = true, CreatedAt = now, ModifiedAt = now, PublishedAt = now,
            });

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        _logger.LogInformation("Seeded sample pages (About, I’m New, What We Believe, Contact).");
    }

    // ── Service times ────────────────────────────────────────────────────────

    private async Task SeedSampleServiceTimesAsync(CancellationToken ct)
    {
        if (await _db.ServiceTimes.AnyAsync(ct).ConfigureAwait(false)) return;
        var now = DateTimeOffset.UtcNow;
        _db.ServiceTimes.AddRange(
            new ServiceTime
            {
                Id = Guid.NewGuid(),
                Name = "Traditional Worship",
                DayOfWeek = DayOfWeek.Sunday,
                StartTime = new TimeOnly(9, 0),
                EndTime = new TimeOnly(10, 10),
                Location = "Sanctuary",
                DisplayOrder = 0,
                IsActive = true,
                CreatedAt = now,
                ModifiedAt = now,
            },
            new ServiceTime
            {
                Id = Guid.NewGuid(),
                Name = "Contemporary Worship",
                DayOfWeek = DayOfWeek.Sunday,
                StartTime = new TimeOnly(11, 0),
                EndTime = new TimeOnly(12, 10),
                Location = "Sanctuary",
                DisplayOrder = 1,
                IsActive = true,
                CreatedAt = now,
                ModifiedAt = now,
            },
            new ServiceTime
            {
                Id = Guid.NewGuid(),
                Name = "Evening Prayer",
                DayOfWeek = DayOfWeek.Sunday,
                StartTime = new TimeOnly(18, 30),
                EndTime = new TimeOnly(19, 30),
                Location = "Chapel",
                DisplayOrder = 2,
                IsActive = true,
                CreatedAt = now,
                ModifiedAt = now,
            });
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        _logger.LogInformation("Seeded sample service times (Traditional, Contemporary, Evening Prayer).");
    }

    // ── Leaders & staff ──────────────────────────────────────────────────────

    private async Task SeedSampleLeadersAsync(CancellationToken ct)
    {
        if (await _db.Leaders.AnyAsync(ct).ConfigureAwait(false)) return;
        var now = DateTimeOffset.UtcNow;
        _db.Leaders.AddRange(
            new Leader
            {
                Id = Guid.NewGuid(),
                FullName = "Daniel Reyes",
                Title = "Lead Pastor",
                Category = "Ministers",
                BioJson = Doc(P("With us since 2017. Husband to Marta, dad to three loud kids. Studied at Trinity.")),
                Email = "daniel@hopecommunity.church",
                DisplayOrder = 0,
                CreatedAt = now,
                ModifiedAt = now,
            },
            new Leader
            {
                Id = Guid.NewGuid(),
                FullName = "Anna Kowalski",
                Title = "Associate Pastor — Care",
                Category = "Ministers",
                BioJson = Doc(P("Hospital chaplain for 12 years before joining staff in 2021. Coffee snob, hospice champion.")),
                Email = "anna@hopecommunity.church",
                DisplayOrder = 1,
                CreatedAt = now,
                ModifiedAt = now,
            },
            new Leader
            {
                Id = Guid.NewGuid(),
                FullName = "Marcus Chen",
                Title = "Worship Director",
                Category = "Staff",
                BioJson = Doc(P("Leads our music team and the choir. Plays piano, banjo, anything with strings really.")),
                Email = "marcus@hopecommunity.church",
                DisplayOrder = 2,
                CreatedAt = now,
                ModifiedAt = now,
            },
            new Leader
            {
                Id = Guid.NewGuid(),
                FullName = "Ruth Adeyemi",
                Title = "Children & Families",
                Category = "Staff",
                BioJson = Doc(P("Runs Sunday school, kids check-in, and the Wednesday family dinners. Former teacher.")),
                Email = "ruth@hopecommunity.church",
                DisplayOrder = 3,
                CreatedAt = now,
                ModifiedAt = now,
            },
            new Leader
            {
                Id = Guid.NewGuid(),
                FullName = "Tom Hartline",
                Title = "Operations",
                Category = "Staff",
                BioJson = Doc(P("Keeps the lights on, the snow plowed, and the spreadsheets honest. 30 years in.")),
                Email = "tom@hopecommunity.church",
                DisplayOrder = 4,
                CreatedAt = now,
                ModifiedAt = now,
            },
            new Leader
            {
                Id = Guid.NewGuid(),
                FullName = "Imani Johnson",
                Title = "Youth Director",
                Category = "Staff",
                BioJson = Doc(P("Joined in 2023. Runs middle school + high school groups, retreats, and mission trips.")),
                Email = "imani@hopecommunity.church",
                DisplayOrder = 5,
                CreatedAt = now,
                ModifiedAt = now,
            });
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        _logger.LogInformation("Seeded sample leaders (6 staff members).");
    }

    // ── News ─────────────────────────────────────────────────────────────────

    private async Task SeedSampleNewsAsync(CancellationToken ct)
    {
        if (await _db.News.AnyAsync(ct).ConfigureAwait(false)) return;
        var now = DateTimeOffset.UtcNow;

        var griefBody = Doc(
            P("Two weeks ago we lost a beloved member of our church. Here is some of what I have been thinking, mostly slowly, about how a community grieves together."),
            P("For two weeks now I have been carrying around a small list of names — people I know who are walking through grief in different forms — and I have not always known what to say to them. There is a particular kind of silence that settles over a community after a loss like this, and the temptation is either to rush to fill it with explanations or to retreat from each other entirely."),
            P("Neither, I think, is what we are being called to."),
            H2("The kind of presence that is asked of us"),
            P("The Christian tradition has, for two thousand years, understood grief as a communal task. We do not grieve alone, and we do not grieve well by ourselves. The book of Lamentations is not a private journal; it is a song, sung together, by people who knew that some sorrows are too heavy to carry one at a time."),
            BQ("“Blessed are those who mourn, for they shall be comforted.” — Matthew 5:4"),
            P("What we owe each other in this season is not advice, and not silence either, but presence. The willingness to sit on a porch, drop off a meal, write a card, say the name of the one we have lost. To not flinch from each other’s tears. To not require speed."),
            P("I will be in the church office most weekday afternoons over the next month. The door is open. We don’t have to talk; we can just sit."),
            P(Ital("Grace and peace,")),
            P(Ital("Daniel")));

        var retreatBody = Doc(
            P("Twenty-two students, four leaders, three days, one cabin without working wifi — and a remarkable amount of honest conversation."),
            P("Every fall we take the youth group to a small camp north of Waterloo. The schedule is loose: hikes, campfires, a few structured talks, and long stretches of unstructured time. That’s where the real conversations happen — late at night, sitting on the dock, when the pressure to perform falls away."),
            P("This year several students opened up about anxiety, loneliness, and the weight of expectations they carry at school. The leaders didn’t have answers for everything. But they stayed present, listened well, and pointed back to a God who is not in a hurry."),
            P("If your student came home quieter than usual, that’s probably a good sign."));

        var groupsBody = Doc(
            P("We are starting six new groups this fall, including two for parents of small children, one for grad students, and a midday group for retirees."),
            P("Small groups are the backbone of our community life. They meet weekly in homes, coffee shops, and the church library. Each group chooses its own study material and sets its own rhythm."),
            P("If you’ve been looking for a way to connect beyond Sunday morning, this is the place to start. Signups are open through the end of the month."));

        var soupKitchenBody = Doc(
            P("The soup kitchen started in 1934, in the basement, with one pot and a borrowed table. During the Depression the church fed anyone who showed up — no questions, no sign-in sheet. It ran five days a week for nearly a decade."),
            P("When the economy recovered the kitchen scaled back, but it never fully closed. Today it operates every Tuesday and Thursday, serving about forty meals each session. The volunteers rotate, but a few have been showing up for over twenty years."),
            P("This fall we’re expanding to three days a week and partnering with the Cedar Falls Community Pantry to offer take-home bags. If you’d like to help, talk to Anna Kowalski or sign up on the volunteer board in the lobby."));

        var prayerBody = Doc(
            P("There is a way of praying for a city that is not about fixing it. It is about holding it — its noise, its need, its beauty — before God and trusting that he is already at work in places we cannot see."),
            P("Each Wednesday at noon a small group gathers in the chapel to pray for Cedar Falls by name: for the schools, the shelters, the city council, the hospital staff, the neighbours we know and the ones we don’t. It lasts about twenty minutes. No one leads. We just pray."),
            P("You are welcome to join us. The chapel is open."));

        _db.News.AddRange(
            new NewsItem
            {
                Id = Guid.NewGuid(), Slug = "a-note-from-pastor-daniel-on-grief",
                Title = "A note from Pastor Daniel on grief",
                BodyJson = griefBody,
                Excerpt = "Two weeks ago we lost a beloved member of our church. Here is some of what I have been thinking about how a community grieves together.",
                IsPublished = true, IsMembersOnly = false,
                CreatedAt = now.AddDays(-2), ModifiedAt = now.AddDays(-2), PublishedAt = now.AddDays(-2),
            },
            new NewsItem
            {
                Id = Guid.NewGuid(), Slug = "reflections-from-the-youth-retreat",
                Title = "Reflections from the youth retreat",
                BodyJson = retreatBody,
                Excerpt = "Twenty-two students, four leaders, three days, one cabin without working wifi — and a remarkable amount of honest conversation.",
                IsPublished = true, IsMembersOnly = false,
                CreatedAt = now.AddDays(-9), ModifiedAt = now.AddDays(-9), PublishedAt = now.AddDays(-9),
            },
            new NewsItem
            {
                Id = Guid.NewGuid(), Slug = "new-small-groups-launching-this-fall",
                Title = "New small groups launching this fall",
                BodyJson = groupsBody,
                Excerpt = "We are starting six new groups this fall, including two for parents of small children, one for grad students, and a midday group for retirees.",
                IsPublished = true, IsMembersOnly = false,
                CreatedAt = now.AddDays(-16), ModifiedAt = now.AddDays(-16), PublishedAt = now.AddDays(-16),
            },
            new NewsItem
            {
                Id = Guid.NewGuid(), Slug = "how-the-soup-kitchen-began",
                Title = "How the soup kitchen began (and where it is going)",
                BodyJson = soupKitchenBody,
                Excerpt = "The soup kitchen started in 1934, in the basement, with one pot and a borrowed table.",
                IsPublished = true, IsMembersOnly = false,
                CreatedAt = now.AddDays(-23), ModifiedAt = now.AddDays(-23), PublishedAt = now.AddDays(-23),
            },
            new NewsItem
            {
                Id = Guid.NewGuid(), Slug = "on-praying-for-our-city",
                Title = "On praying for our city",
                BodyJson = prayerBody,
                Excerpt = "There is a way of praying for a city that is not about fixing it. It is about holding it before God.",
                IsPublished = true, IsMembersOnly = false,
                CreatedAt = now.AddDays(-30), ModifiedAt = now.AddDays(-30), PublishedAt = now.AddDays(-30),
            });

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        _logger.LogInformation("Seeded sample news items (5).");
    }

    // ── Sermons ──────────────────────────────────────────────────────────────

    private async Task SeedSampleSermonContentAsync(CancellationToken ct)
    {
        if (await _db.SermonSeries.AnyAsync(ct).ConfigureAwait(false)
            || await _db.Sermons.AnyAsync(ct).ConfigureAwait(false)) return;

        var now = DateTimeOffset.UtcNow;

        var danielReyes = await _db.Leaders.FirstOrDefaultAsync(l => l.FullName == "Daniel Reyes", ct).ConfigureAwait(false);
        var annaKowalski = await _db.Leaders.FirstOrDefaultAsync(l => l.FullName == "Anna Kowalski", ct).ConfigureAwait(false);

        // ── Series: Made to Belong (Gospel of Luke, 6 parts) ─────────
        var madeToBelung = new SermonSeries
        {
            Id = Guid.NewGuid(),
            Slug = "made-to-belong",
            Title = "Made to Belong",
            DescriptionJson = Doc(P("Six weeks in the Gospel of Luke on belonging — what it costs, what it gives, and the kind of table Jesus keeps setting.")),
            StartDate = DateOnly.FromDateTime(now.AddDays(-28).UtcDateTime),
            CreatedAt = now,
            ModifiedAt = now,
        };

        // ── Series: A Year in the Psalms ─────────────────────────────
        var yearInPsalms = new SermonSeries
        {
            Id = Guid.NewGuid(),
            Slug = "a-year-in-the-psalms",
            Title = "A Year in the Psalms",
            DescriptionJson = Doc(P("Songs sung on the road to Jerusalem — and to God.")),
            StartDate = DateOnly.FromDateTime(now.AddDays(-56).UtcDateTime),
            CreatedAt = now,
            ModifiedAt = now,
        };

        _db.SermonSeries.AddRange(madeToBelung, yearInPsalms);

        // Pre-generate IDs so we can attach ScriptureReferences after save.
        var s1Id = Guid.NewGuid(); var s2Id = Guid.NewGuid();
        var s3Id = Guid.NewGuid(); var s4Id = Guid.NewGuid();
        var s5Id = Guid.NewGuid(); var s6Id = Guid.NewGuid();
        var p1Id = Guid.NewGuid(); var p2Id = Guid.NewGuid();

        // ── Made to Belong sermons (all 6 published for demo) ────────
        _db.Sermons.AddRange(
            new Sermon
            {
                Id = s1Id, Slug = "the-cost-of-belonging",
                Title = "The Cost of Belonging",
                DescriptionJson = Doc(P("What does it actually cost to follow Jesus into a community that doesn’t look like the one you’d design? Luke 14 lays it out plainly — and the invitation is still worth it.")),
                YouTubeVideoId = "dQw4w9WgXcQ",
                DurationSeconds = 40 * 60,
                SpeakerLeaderId = danielReyes?.Id,
                SpeakerNameFreeText = "Daniel Reyes",
                SermonSeriesId = madeToBelung.Id,
                IsPublished = true,
                PublishedAt = now.AddDays(-28),
                YouTubePublishedAt = now.AddDays(-28),
                CreatedAt = now.AddDays(-28), ModifiedAt = now.AddDays(-28),
            },
            new Sermon
            {
                Id = s2Id, Slug = "lost-and-found",
                Title = "Lost & Found",
                DescriptionJson = Doc(P("A coin, a sheep, a searchlight through the dark — three images of a God who refuses to write anyone off. What does it mean that heaven throws a party over one person who comes home?")),
                YouTubeVideoId = "9bZkp7q19f0",
                DurationSeconds = 36 * 60,
                SpeakerLeaderId = annaKowalski?.Id,
                SpeakerNameFreeText = "Anna Kowalski",
                SermonSeriesId = madeToBelung.Id,
                IsPublished = true,
                PublishedAt = now.AddDays(-21),
                YouTubePublishedAt = now.AddDays(-21),
                CreatedAt = now.AddDays(-21), ModifiedAt = now.AddDays(-21),
            },
            new Sermon
            {
                Id = s3Id, Slug = "the-long-way-home",
                Title = "The Long Way Home",
                DescriptionJson = Doc(P("The prodigal son story is not about a wayward kid. It’s about a father who runs. And an older brother who can’t bring himself to go inside. Which one are you?")),
                YouTubeVideoId = "kJQP7kiw5Fk",
                DurationSeconds = 42 * 60,
                SpeakerLeaderId = danielReyes?.Id,
                SpeakerNameFreeText = "Daniel Reyes",
                SermonSeriesId = madeToBelung.Id,
                IsPublished = true,
                PublishedAt = now.AddDays(-14),
                YouTubePublishedAt = now.AddDays(-14),
                CreatedAt = now.AddDays(-14), ModifiedAt = now.AddDays(-14),
            },
            new Sermon
            {
                Id = s4Id, Slug = "a-table-with-room",
                Title = "A Table With Room",
                DescriptionJson = Doc(P("Jesus tells a parable about a feast nobody wants to come to — and a host whose generosity refuses to take “no” for the final word. What does it mean to belong to a kingdom that won’t stop setting places at the table?")),
                YouTubeVideoId = "fLexgOxsZu0",
                DurationSeconds = 38 * 60,
                SpeakerLeaderId = danielReyes?.Id,
                SpeakerNameFreeText = "Daniel Reyes",
                SermonSeriesId = madeToBelung.Id,
                IsPublished = true,
                PublishedAt = now.AddDays(-7),
                YouTubePublishedAt = now.AddDays(-7),
                CreatedAt = now.AddDays(-7), ModifiedAt = now.AddDays(-7),
            },
            new Sermon
            {
                Id = s5Id, Slug = "outside-the-camp",
                Title = "Outside The Camp",
                DescriptionJson = Doc(P("Hebrews tells us Jesus suffered outside the gate. What does it look like to follow him there — to the margins, the edges, the places the religious world avoids?")),
                YouTubeVideoId = "L_jWHffIx5E",
                DurationSeconds = 41 * 60,
                SpeakerLeaderId = danielReyes?.Id,
                SpeakerNameFreeText = "Daniel Reyes",
                SermonSeriesId = madeToBelung.Id,
                IsPublished = true,
                PublishedAt = now.AddDays(7),
                YouTubePublishedAt = now.AddDays(7),
                CreatedAt = now, ModifiedAt = now,
            },
            new Sermon
            {
                Id = s6Id, Slug = "coming-home",
                Title = "Coming Home",
                DescriptionJson = Doc(P("The final week of Made to Belong. What does it look like to stay? To belong not just in the moment of welcome but in the long, ordinary middle?")),
                YouTubeVideoId = "YR5ApYxkU-U",
                DurationSeconds = 39 * 60,
                SpeakerLeaderId = annaKowalski?.Id,
                SpeakerNameFreeText = "Anna Kowalski",
                SermonSeriesId = madeToBelung.Id,
                IsPublished = true,
                PublishedAt = now.AddDays(14),
                YouTubePublishedAt = now.AddDays(14),
                CreatedAt = now, ModifiedAt = now,
            });

        // ── A Year in the Psalms sermons ─────────────────────────────
        _db.Sermons.AddRange(
            new Sermon
            {
                Id = p1Id, Slug = "psalm-73-until-i-entered",
                Title = "Psalm 73 — Until I Entered",
                DescriptionJson = Doc(P("The psalmist nearly loses his faith watching the wicked prosper. Then he walks into the sanctuary. What changes when we stop comparing and start worshipping?")),
                YouTubeVideoId = "hT_nvWreIhg",
                DurationSeconds = 39 * 60,
                SpeakerLeaderId = danielReyes?.Id,
                SpeakerNameFreeText = "Daniel Reyes",
                SermonSeriesId = yearInPsalms.Id,
                IsPublished = true,
                PublishedAt = now.AddDays(-42),
                YouTubePublishedAt = now.AddDays(-42),
                CreatedAt = now.AddDays(-42), ModifiedAt = now.AddDays(-42),
            },
            new Sermon
            {
                Id = p2Id, Slug = "psalm-84-a-day-in-your-courts",
                Title = "Psalm 84 — A Day in Your Courts",
                DescriptionJson = Doc(P("Better is one day in your courts than a thousand elsewhere. The sons of Korah sing about longing, pilgrimage, and the God who doesn’t make us earn our welcome.")),
                YouTubeVideoId = "JGwWNGJdvx8",
                DurationSeconds = 34 * 60,
                SpeakerLeaderId = annaKowalski?.Id,
                SpeakerNameFreeText = "Anna Kowalski",
                SermonSeriesId = yearInPsalms.Id,
                IsPublished = true,
                PublishedAt = now.AddDays(-35),
                YouTubePublishedAt = now.AddDays(-35),
                CreatedAt = now.AddDays(-35), ModifiedAt = now.AddDays(-35),
            });

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        // ── Scripture references (one per sermon) ────────────────────
        ScriptureReference Ref(Guid sermonId, BibleBook book, int ch, int? vs, int? chEnd, int? veEnd)
            => new()
            {
                Id = Guid.NewGuid(), ParentEntityType = "Sermon", ParentEntityId = sermonId,
                Book = book, ChapterStart = ch, VerseStart = vs, ChapterEnd = chEnd, VerseEnd = veEnd,
                DisplayOrder = 0, CreatedAt = now, ModifiedAt = now,
            };

        _db.ScriptureReferences.AddRange(
            Ref(s1Id, BibleBook.Luke, 14, 25, 14, 33),   // The Cost of Belonging — Luke 14:25-33
            Ref(s2Id, BibleBook.Luke, 15, 1, 15, 10),    // Lost & Found — Luke 15:1-10
            Ref(s3Id, BibleBook.Luke, 15, 11, 15, 32),   // The Long Way Home — Luke 15:11-32
            Ref(s4Id, BibleBook.Luke, 14, 15, 14, 24),   // A Table With Room — Luke 14:15-24
            Ref(s5Id, BibleBook.Hebrews, 13, 10, 13, 14),// Outside The Camp — Hebrews 13:10-14
            Ref(s6Id, BibleBook.Luke, 15, 11, 15, 32),   // Coming Home — Luke 15:11-32
            Ref(p1Id, BibleBook.Psalms, 73, null, null, null), // Psalm 73
            Ref(p2Id, BibleBook.Psalms, 84, null, null, null)  // Psalm 84
        );

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        _logger.LogInformation("Seeded sample sermon series + sermons + scripture references.");
    }

    // ── Events ───────────────────────────────────────────────────────────────

    private async Task SeedSampleEventsAsync(CancellationToken ct)
    {
        if (await _db.Events.AnyAsync(ct).ConfigureAwait(false)) return;
        var now = DateTimeOffset.UtcNow;

        var newcomersLunch = new Event
        {
            Id = Guid.NewGuid(),
            Slug = "newcomers-lunch",
            Title = "Newcomers Lunch",
            DescriptionJson = Doc(
                P("A simple lunch in the fellowship hall for anyone who has visited recently. Come meet the pastors and a few other newcomers — no follow-up commitment, no church-y tour, just food."),
                H2("What to expect"),
                P("Walk in through the front doors after the 11:00 service — there will be a sign — and follow the smell of food to the fellowship hall. Lunch is on us. Kids are welcome (we’ll have a kids’ table set up)."),
                H2("What’s on the menu"),
                P("Soup (one veg, one not), good bread, salad, and a cookie tray that has historically been the most-discussed item of every newcomers lunch. Coffee, tea, and lemonade."),
                H2("Do I need to RSVP?"),
                P("It helps us plan, but you can absolutely just show up. The form below sends us a heads-up; if you fill it out we’ll know roughly how many to expect.")),
            StartsAt = NextDayOfWeek(now, DayOfWeek.Sunday).AddDays(7).AddHours(12).AddMinutes(30),
            EndsAt = NextDayOfWeek(now, DayOfWeek.Sunday).AddDays(7).AddHours(14),
            Location = "Fellowship Hall · 412 Maple Ave",
            Visibility = EventVisibility.Public,
            RegistrationMode = EventRegistrationMode.RsvpOptional,
            Capacity = 40,
            WaitlistEnabled = false,
            IsPublished = true,
            CreatedAt = now, ModifiedAt = now,
        };

        var midweekBibleStudy = new Event
        {
            Id = Guid.NewGuid(),
            Slug = "midweek-bible-study",
            Title = "Midweek Bible Study",
            DescriptionJson = Doc(
                P("We’re working through the Gospel of Mark, slowly. Newcomers always welcome."),
                P("Bring a Bible if you have one — we’ll provide handouts. The group is led by Pastor Daniel and meets in the library. Childcare is available.")),
            StartsAt = NextDayOfWeek(now, DayOfWeek.Wednesday).AddHours(19),
            EndsAt = NextDayOfWeek(now, DayOfWeek.Wednesday).AddHours(20).AddMinutes(30),
            Location = "Library",
            Visibility = EventVisibility.Public,
            RecurrenceRule = "FREQ=WEEKLY;BYDAY=WE",
            IsPublished = true,
            CreatedAt = now, ModifiedAt = now,
        };

        var choirTryouts = new Event
        {
            Id = Guid.NewGuid(),
            Slug = "choir-tryouts",
            Title = "Choir Tryouts",
            DescriptionJson = Doc(
                P("Open auditions for the Christmas season. No experience required, just willingness."),
                P("Marcus Chen leads the choir. We rehearse Thursday evenings. The Christmas concert is December 15. If you can carry a tune — or even if you’re not sure — come try.")),
            StartsAt = NextDayOfWeek(now, DayOfWeek.Sunday).AddDays(14).AddHours(13),
            EndsAt = NextDayOfWeek(now, DayOfWeek.Sunday).AddDays(14).AddHours(15),
            Location = "Music Room",
            Visibility = EventVisibility.Public,
            IsPublished = true,
            CreatedAt = now, ModifiedAt = now,
        };

        var harvestVolunteerDay = new Event
        {
            Id = Guid.NewGuid(),
            Slug = "harvest-volunteer-day",
            Title = "Harvest Volunteer Day",
            DescriptionJson = Doc(
                P("Helping a few neighbours with leaves, gutters, and groceries. Bring gloves."),
                P("We’ll meet in the church parking lot at 9:00 AM, split into teams, and head out. Back by noon. All ages welcome — there’s a job for everyone.")),
            StartsAt = NextDayOfWeek(now, DayOfWeek.Saturday).AddDays(14).AddHours(9),
            EndsAt = NextDayOfWeek(now, DayOfWeek.Saturday).AddDays(14).AddHours(12),
            Location = "Off-site (meet at church parking lot)",
            Visibility = EventVisibility.Public,
            RegistrationMode = EventRegistrationMode.RsvpOptional,
            IsPublished = true,
            CreatedAt = now, ModifiedAt = now,
        };

        var allSaintsVigil = new Event
        {
            Id = Guid.NewGuid(),
            Slug = "all-saints-vigil",
            Title = "All Saints Vigil",
            DescriptionJson = Doc(
                P("A quiet candlelight service remembering those we have loved and lost this year. Open to the whole community — bring a name, a memory, a candle."),
                P("The vigil lasts about ninety minutes. We’ll read names, sing a few hymns, sit in silence, and light candles together. Childcare is available but children are also welcome in the service.")),
            StartsAt = NextDayOfWeek(now, DayOfWeek.Saturday).AddDays(21).AddHours(19),
            EndsAt = NextDayOfWeek(now, DayOfWeek.Saturday).AddDays(21).AddHours(20).AddMinutes(30),
            Location = "Sanctuary · 412 Maple Ave",
            Visibility = EventVisibility.Public,
            RegistrationMode = EventRegistrationMode.RsvpOptional,
            IsPublished = true,
            CreatedAt = now, ModifiedAt = now,
        };

        var allSaintsSunday = new Event
        {
            Id = Guid.NewGuid(),
            Slug = "all-saints-sunday",
            Title = "All Saints Sunday",
            DescriptionJson = Doc(
                P("We read the names. We light candles. We give thanks for the great cloud of witnesses."),
                P("Both the 9:00 and 11:00 services will include a special liturgy for All Saints. If you’d like a loved one’s name read aloud, please submit it through the church office by Wednesday.")),
            StartsAt = NextDayOfWeek(now, DayOfWeek.Sunday).AddDays(28).AddHours(9),
            EndsAt = NextDayOfWeek(now, DayOfWeek.Sunday).AddDays(28).AddHours(12).AddMinutes(15),
            Location = "Sanctuary",
            Visibility = EventVisibility.Public,
            IsPublished = true,
            CreatedAt = now, ModifiedAt = now,
        };

        _db.Events.AddRange(newcomersLunch, midweekBibleStudy, choirTryouts,
            harvestVolunteerDay, allSaintsVigil, allSaintsSunday);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        _logger.LogInformation("Seeded sample events (6).");
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

    // ── Groups ───────────────────────────────────────────────────────────────

    private async Task SeedSampleGroupsAsync(CancellationToken ct)
    {
        if (await _db.Groups.AnyAsync(ct).ConfigureAwait(false)) return;
        var now = DateTimeOffset.UtcNow;

        var bibleStudy = new Group
        {
            Id = Guid.NewGuid(),
            Slug = "wednesday-bible-study",
            Name = "Wednesday Bible Study",
            DescriptionJson = Doc(
                P("A midweek group working through one book of Scripture each term. Currently in the Gospel of Mark."),
                P("Led by Pastor Daniel. Newcomers are always welcome — no preparation required. Childcare provided.")),
            MeetingInfo = "Wednesdays · 7:00 PM · Library",
            Visibility = GroupVisibility.Public,
            Joinability = GroupJoinability.Open,
            RequiresMessageOnJoinRequest = MessageOnJoinRequest.Optional,
            RosterVisibility = RosterVisibility.AllGroupMembers,
            IsActive = true,
            CreatedAt = now, ModifiedAt = now,
        };

        var hospitalityTeam = new Group
        {
            Id = Guid.NewGuid(),
            Slug = "hospitality-team",
            Name = "Hospitality Team",
            DescriptionJson = Doc(
                P("The people who make Sunday mornings feel like home. Greeters, coffee brewers, setup crew, and the folks in maroon name tags."),
                P("We meet briefly before the 9:00 service to set up and debrief after the 11:00 service. It’s a low-key, high-impact way to serve.")),
            MeetingInfo = "Sundays · pre-service · Lobby",
            Visibility = GroupVisibility.Public,
            Joinability = GroupJoinability.Open,
            RequiresMessageOnJoinRequest = MessageOnJoinRequest.Optional,
            RosterVisibility = RosterVisibility.LeadersOnly,
            IsActive = true,
            CreatedAt = now, ModifiedAt = now,
        };

        var choir = new Group
        {
            Id = Guid.NewGuid(),
            Slug = "choir",
            Name = "Choir",
            DescriptionJson = Doc(
                P("Led by Marcus Chen. We sing at the 9:00 traditional service and prepare a Christmas concert each December."),
                P("Rehearsals are Thursday evenings. All skill levels welcome — if you enjoy singing, you belong here.")),
            MeetingInfo = "Thursdays · 6:30 PM · Music Room",
            ContactEmail = "marcus@hopecommunity.church",
            Visibility = GroupVisibility.Public,
            Joinability = GroupJoinability.Open,
            RequiresMessageOnJoinRequest = MessageOnJoinRequest.Optional,
            RosterVisibility = RosterVisibility.AllGroupMembers,
            IsActive = true,
            CreatedAt = now, ModifiedAt = now,
        };

        _db.Groups.AddRange(bibleStudy, hospitalityTeam, choir);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        _logger.LogInformation("Seeded sample groups (Wednesday Bible Study, Hospitality Team, Choir).");
    }

    // ── Blog posts ───────────────────────────────────────────────────────────

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
            BodyJson = Doc(
                P("We’re starting a regular space for devotionals, sermon notes, and reflections from the pastors. Subscribe via the church newsletter to be notified when new posts go up."),
                P("This blog is a place for the kind of writing that doesn’t fit in a Sunday sermon — longer reflections, recommended reading, updates from mission partners, and the occasional recipe from the fellowship hall kitchen."),
                P("If you’d like to contribute, talk to Pastor Daniel or any of the staff. We’d love to hear your voice here too.")),
            Excerpt = "A space for devotionals, sermon notes, and reflections.",
            AuthorUserId = admin.Id,
            Category = "Announcements",
            IsPublished = true,
            IsPinned = true,
            PublishedAt = now,
            ReadingTimeMinutes = 1,
            CreatedAt = now,
            ModifiedAt = now,
            ModifiedByUserId = admin.Id,
        };
        _db.BlogPosts.Add(welcome);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        _logger.LogInformation("Seeded sample blog post (welcome).");
    }

    // ── Email templates ──────────────────────────────────────────────────────

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
                AvailableMergeFieldsJson = JsonSerializer.Serialize(vars),
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
                CreatedAt = now,
                ModifiedAt = now,
            },
            new EventVolunteerRole
            {
                Id = Guid.NewGuid(),
                EventId = firstEvent.Id,
                RoleName = "Greeter",
                Description = "Welcome attendees, hand out nametags.",
                SlotsNeeded = 2,
                DisplayOrder = 1,
                CreatedAt = now,
                ModifiedAt = now,
            });
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        _logger.LogInformation("Seeded sample volunteer roles on event {Id}.", firstEvent.Id);
    }

    // ── Demo content ─────────────────────────────────────────────────────────

    private async Task SeedSampleClassesAsync(CancellationToken ct)
    {
        if (await _db.ClassSlots.AnyAsync(ct).ConfigureAwait(false)) return;
        var now = DateTimeOffset.UtcNow;
        var slotAdult = new ClassSlot
        {
            Id = Guid.NewGuid(),
            Slug = "sunday-morning-bible-study",
            Name = "Sunday Morning Bible Study",
            AudienceAgeGroup = "Adults",
            GeneralMeetingTime = "Sundays · 9:00am",
            DefaultRoom = "Fellowship Hall",
            DescriptionJson = ParaJson("Open Bible study working through one book of Scripture each term. Coffee provided; everyone welcome."),
            IsActive = true,
            DisplayOrder = 0,
            CreatedAt = now,
            ModifiedAt = now,
        };
        var slotKids = new ClassSlot
        {
            Id = Guid.NewGuid(),
            Slug = "kids-discovery-class",
            Name = "Kids Discovery Class",
            AudienceAgeGroup = "Children (K-5)",
            GeneralMeetingTime = "Sundays · 10:30am",
            DefaultRoom = "Children's Wing",
            DescriptionJson = ParaJson("Age-appropriate Bible stories, songs, and crafts during the second service."),
            IsActive = true,
            DisplayOrder = 1,
            CreatedAt = now,
            ModifiedAt = now,
        };
        _db.ClassSlots.AddRange(slotAdult, slotKids);

        var termStart = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7));
        var termEnd = termStart.AddDays(70);
        _db.ClassOfferings.AddRange(
            new ClassOffering
            {
                Id = Guid.NewGuid(),
                ClassSlotId = slotAdult.Id,
                Subject = "The Gospel of Mark",
                DescriptionJson = ParaJson("A ten-week walk through Mark's gospel. Bring a Bible; we'll provide handouts."),
                StartDate = termStart,
                EndDate = termEnd,
                TeacherFreeText = "Pastor Daniel",
                CreatedAt = now,
                ModifiedAt = now,
            },
            new ClassOffering
            {
                Id = Guid.NewGuid(),
                ClassSlotId = slotKids.Id,
                Subject = "Heroes of the Old Testament",
                DescriptionJson = ParaJson("Ten weeks of stories from Abraham, Moses, David, and more."),
                StartDate = termStart,
                EndDate = termEnd,
                TeacherFreeText = "Children's Ministry Team",
                CreatedAt = now,
                ModifiedAt = now,
            });
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        _logger.LogInformation("Seeded sample classes (2 slots, 2 current offerings).");
    }

    private async Task SeedSampleDocumentsAsync(CancellationToken ct)
    {
        if (await _db.Documents.AnyAsync(ct).ConfigureAwait(false)) return;
        var now = DateTimeOffset.UtcNow;
        _db.Documents.AddRange(
            new Document
            {
                Id = Guid.NewGuid(),
                Title = "Sunday bulletin (template)",
                Description = "Weekly worship guide template — replace with your church's actual bulletin.",
                Category = "Bulletins",
                BlobUrl = "placeholder://bulletins/template.pdf",
                OriginalFilename = "bulletin-template.pdf",
                SizeBytes = 0,
                IsPublished = true,
                IsMembersOnly = false,
                CreatedAt = now,
                ModifiedAt = now,
            },
            new Document
            {
                Id = Guid.NewGuid(),
                Title = "New member welcome packet",
                Description = "Onboarding information for new members.",
                Category = "Forms",
                BlobUrl = "placeholder://forms/welcome-packet.pdf",
                OriginalFilename = "welcome-packet.pdf",
                SizeBytes = 0,
                IsPublished = true,
                IsMembersOnly = true,
                CreatedAt = now,
                ModifiedAt = now,
            });
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        _logger.LogInformation("Seeded sample documents (placeholder BlobUrls).");
    }

    private async Task SeedSampleDirectoryMembersAsync(CancellationToken ct)
    {
        var existing = await _userManager.FindByEmailAsync("sample.member@credocms.local").ConfigureAwait(false);
        if (existing is not null) return;

        var now = DateTimeOffset.UtcNow;
        var alice = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "sample.member@credocms.local",
            Email = "sample.member@credocms.local",
            EmailConfirmed = false,
            FirstName = "Alice",
            LastName = "Sample",
            IsActive = true,
            IsListedInDirectory = true,
            ShowEmailInDirectory = false,
            ShowPhoneInDirectory = false,
            ShowPhotoInDirectory = true,
            PublicAuthorBio = "Member since 2019. Serves on the welcome team.",
            CreatedAt = now,
        };
        var ben = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "sample.member2@credocms.local",
            Email = "sample.member2@credocms.local",
            EmailConfirmed = false,
            FirstName = "Ben",
            LastName = "Example",
            IsActive = true,
            IsListedInDirectory = true,
            ShowEmailInDirectory = true,
            ShowPhoneInDirectory = false,
            ShowPhotoInDirectory = false,
            PublicAuthorBio = "Husband, father, accountant. Helps run the small-groups program.",
            CreatedAt = now,
        };

        var aliceResult = await _userManager.CreateAsync(alice).ConfigureAwait(false);
        var benResult = await _userManager.CreateAsync(ben).ConfigureAwait(false);
        if (!aliceResult.Succeeded || !benResult.Succeeded) return;

        await _userManager.AddToRoleAsync(alice, SystemConstants.Roles.Member).ConfigureAwait(false);
        await _userManager.AddToRoleAsync(ben, SystemConstants.Roles.Member).ConfigureAwait(false);
        _logger.LogInformation("Seeded sample directory members (cannot sign in; replace with real invitations).");
    }

    private async Task SeedSamplePrayerRequestsAsync(CancellationToken ct)
    {
        if (await _db.PrayerRequests.AnyAsync(ct).ConfigureAwait(false)) return;
        var admin = await _userManager.FindByEmailAsync(_identitySeed.DefaultAdminEmail).ConfigureAwait(false);
        if (admin is null) return;

        var now = DateTimeOffset.UtcNow;
        _db.PrayerRequests.AddRange(
            new PrayerRequest
            {
                Id = Guid.NewGuid(),
                Title = "Healing for a friend",
                BodyJson = ParaJson("Please pray for healing for a friend recovering from surgery."),
                SubmittedByUserId = admin.Id,
                IsAnonymous = false,
                Status = PrayerRequestStatus.Active,
                CreatedAt = now,
                ModifiedAt = now,
            },
            new PrayerRequest
            {
                Id = Guid.NewGuid(),
                Title = "Wisdom for a job change",
                BodyJson = ParaJson("Discerning a major career decision in the next month. Asking for clarity."),
                SubmittedByUserId = admin.Id,
                IsAnonymous = true,
                Status = PrayerRequestStatus.Active,
                CreatedAt = now.AddDays(-2),
                ModifiedAt = now.AddDays(-2),
            });
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        _logger.LogInformation("Seeded sample prayer requests.");
    }

    private async Task SeedSampleConnectCardsAsync(CancellationToken ct)
    {
        if (await _db.ConnectCardSubmissions.AnyAsync(ct).ConfigureAwait(false)) return;
        var now = DateTimeOffset.UtcNow;
        _db.ConnectCardSubmissions.AddRange(
            new ConnectCardSubmission
            {
                Id = Guid.NewGuid(),
                Name = "Visitor One",
                Email = "visitor.one@example.org",
                Phone = null,
                IsFirstTimeVisitor = true,
                HowDidYouHear = "Friend invited me",
                Comments = "Came on Sunday and enjoyed it. Would like more info on small groups.",
                Status = ConnectCardStatus.New,
                SubmittedAt = now.AddDays(-1),
                ModifiedAt = now.AddDays(-1),
            },
            new ConnectCardSubmission
            {
                Id = Guid.NewGuid(),
                Name = "Visitor Two",
                Email = "visitor.two@example.org",
                Phone = "+1-555-0124",
                IsFirstTimeVisitor = false,
                HowDidYouHear = "Drove by, saw the sign",
                Comments = "Interested in volunteering with the food pantry.",
                Status = ConnectCardStatus.FollowUpNeeded,
                SubmittedAt = now.AddDays(-3),
                ModifiedAt = now.AddDays(-3),
            });
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        _logger.LogInformation("Seeded sample connect card submissions.");
    }

    // ── ProseMirror / TipTap JSON builders ───────────────────────────────────

    private static string Doc(params object[] nodes)
        => JsonSerializer.Serialize(new { type = "doc", content = nodes });

    private static object H2(string text)
        => new { type = "heading", attrs = new { level = 2 }, content = new object[] { Txt(text) } };

    private static object H3(string text)
        => new { type = "heading", attrs = new { level = 3 }, content = new object[] { Txt(text) } };

    private static object P(string text)
        => new { type = "paragraph", content = new object[] { Txt(text) } };

    private static object P(params object[] inline)
        => new { type = "paragraph", content = inline };

    private static object BQ(string text)
        => new { type = "blockquote", content = new object[] { P(text) } };

    private static object Txt(string text) => new { type = "text", text };
    private static object Bold(string text) => new { type = "text", marks = new[] { new { type = "bold" } }, text };
    private static object Ital(string text) => new { type = "text", marks = new[] { new { type = "italic" } }, text };

    private static string ParaJson(string text) => Doc(P(text));

    private static Page SimplePage(string slug, string title, DateTimeOffset now,
        string paragraph, bool isSystem = false)
    {
        return new Page
        {
            Id = Guid.NewGuid(),
            Slug = slug,
            Title = title,
            BodyJson = Doc(P(paragraph)),
            Excerpt = paragraph,
            IsPublished = true,
            IsMembersOnly = false,
            IsSystemPage = isSystem,
            CreatedAt = now,
            ModifiedAt = now,
            PublishedAt = now,
        };
    }
}
