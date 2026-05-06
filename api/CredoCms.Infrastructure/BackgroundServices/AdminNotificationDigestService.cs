using CredoCms.Application.Common;
using CredoCms.Application.Email;
using CredoCms.Application.SiteSettingsManagement;
using CredoCms.Domain.ConnectCard;
using CredoCms.Domain.Email;
using CredoCms.Domain.Groups;
using CredoCms.Domain.Identity;
using CredoCms.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CredoCms.Infrastructure.BackgroundServices;

/// <summary>
/// Phase 5 R11. Ticks every 5 minutes; for each Editor/Administrator,
/// computes the count of unacknowledged Connect Card submissions and
/// pending Group join requests since their last digest, and sends a
/// digest email when the per-user frequency window has elapsed.
///
/// <para>Default frequency is <see cref="AdminNotificationFrequency.Every30Minutes"/>
/// from <see cref="Domain.Settings.SiteSettings.AdminNotificationFrequency"/>;
/// per-admin override on the user profile beats the default (Phase 5 R15
/// wires the SPA toggle).</para>
/// </summary>
public sealed class AdminNotificationDigestService : BackgroundService
{
    private static readonly TimeSpan TickInterval = TimeSpan.FromMinutes(5);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AdminNotificationDigestService> _logger;

    public AdminNotificationDigestService(IServiceScopeFactory scopeFactory, ILogger<AdminNotificationDigestService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AdminNotificationDigestService started, tick={Tick}", TickInterval);
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await TickAsync(stoppingToken).ConfigureAwait(false); }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "AdminNotificationDigestService tick threw");
            }
            await Task.Delay(TickInterval, stoppingToken).ConfigureAwait(false);
        }
    }

    private async Task TickAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var settings = scope.ServiceProvider.GetRequiredService<ISiteSettingsRepository>();
        var lastSentRepo = scope.ServiceProvider.GetRequiredService<IAdminNotificationLastSentRepository>();
        var email = scope.ServiceProvider.GetRequiredService<IEmailService>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var s = await settings.GetAsync(ct).ConfigureAwait(false);
        if (s.AdminNotificationFrequency == AdminNotificationFrequency.Off) return;

        var freqWindow = WindowFor(s.AdminNotificationFrequency);
        var now = DateTimeOffset.UtcNow;

        // Find every Editor + Administrator. Members are excluded.
        var admins = await GetAdminsAsync(userManager, ct).ConfigureAwait(false);

        foreach (var u in admins)
        {
            await ProcessCategoryAsync(
                AdminNotificationCategory.ConnectCardSubmissions,
                u, db, lastSentRepo, email, freqWindow, now, ct).ConfigureAwait(false);

            await ProcessCategoryAsync(
                AdminNotificationCategory.GroupJoinRequests,
                u, db, lastSentRepo, email, freqWindow, now, ct).ConfigureAwait(false);
        }
    }

    private static async Task<List<ApplicationUser>> GetAdminsAsync(UserManager<ApplicationUser> users, CancellationToken ct)
    {
        var admins = await users.GetUsersInRoleAsync("Administrator").ConfigureAwait(false);
        var editors = await users.GetUsersInRoleAsync("Editor").ConfigureAwait(false);
        return admins.Concat(editors)
            .Where(u => u.IsActive && !string.IsNullOrWhiteSpace(u.Email))
            .DistinctBy(u => u.Id)
            .ToList();
    }

    private async Task ProcessCategoryAsync(
        AdminNotificationCategory category,
        ApplicationUser admin,
        ApplicationDbContext db,
        IAdminNotificationLastSentRepository lastSentRepo,
        IEmailService email,
        TimeSpan freqWindow,
        DateTimeOffset now,
        CancellationToken ct)
    {
        var lastSent = await lastSentRepo.GetAsync(admin.Id, category, ct).ConfigureAwait(false);
        var since = lastSent?.LastSentAt ?? DateTimeOffset.MinValue;

        // Only send if the freq-window has elapsed since the last digest.
        if (lastSent is not null && now - lastSent.LastSentAt < freqWindow) return;

        var (count, summary) = category switch
        {
            AdminNotificationCategory.ConnectCardSubmissions
                => await ConnectCardSummaryAsync(db, since, ct).ConfigureAwait(false),
            AdminNotificationCategory.GroupJoinRequests
                => await GroupJoinSummaryAsync(db, since, ct).ConfigureAwait(false),
            _ => (0, string.Empty),
        };

        if (count == 0) return;

        var subject = category == AdminNotificationCategory.ConnectCardSubmissions
            ? $"{count} new connect card{(count == 1 ? "" : "s")} pending review"
            : $"{count} new group join request{(count == 1 ? "" : "s")} pending review";

        var msg = new EmailMessage(
            ToAddress: admin.Email!,
            ToName: admin.DisplayName,
            Subject: subject,
            HtmlBody: $"<p>Hi {System.Web.HttpUtility.HtmlEncode(admin.FirstName)},</p>" + summary,
            PlainTextBody: $"Hi {admin.FirstName},\n\n" + summary,
            UserId: admin.Id,
            // Digests are operational/transactional — bypass suppression
            // because admins explicitly opted into administrative duties.
            Category: EmailCategory.Transactional);

        try
        {
            await email.SendTransactionalAsync(msg, ct).ConfigureAwait(false);
            await lastSentRepo.UpsertAsync(new AdminNotificationLastSent
            {
                Id = Guid.NewGuid(),
                UserId = admin.Id,
                NotificationCategory = category,
                LastSentAt = now,
            }, ct).ConfigureAwait(false);
            _logger.LogInformation("AdminNotificationDigestService sent {Category} digest ({Count}) to {Admin}",
                category, count, admin.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Digest send failed for {Admin} {Category}", admin.Email, category);
        }
    }

    private static async Task<(int Count, string Summary)> ConnectCardSummaryAsync(
        ApplicationDbContext db, DateTimeOffset since, CancellationToken ct)
    {
        var rows = await db.ConnectCardSubmissions
            .Where(c => c.SubmittedAt > since && c.Status == ConnectCardStatus.New)
            .OrderByDescending(c => c.SubmittedAt)
            .Take(20)
            .Select(c => new { c.Name, c.Email, c.SubmittedAt })
            .ToListAsync(ct).ConfigureAwait(false);
        if (rows.Count == 0) return (0, string.Empty);

        var sb = new System.Text.StringBuilder();
        sb.Append($"<p>You have {rows.Count} new connect card submission{(rows.Count == 1 ? "" : "s")}:</p><ul>");
        foreach (var r in rows)
        {
            sb.Append($"<li>{System.Web.HttpUtility.HtmlEncode(r.Name)}");
            if (!string.IsNullOrWhiteSpace(r.Email))
                sb.Append($" — {System.Web.HttpUtility.HtmlEncode(r.Email)}");
            sb.Append("</li>");
        }
        sb.Append("</ul><p><a href=\"/admin/connect-cards\">Review in admin</a></p>");
        return (rows.Count, sb.ToString());
    }

    private static async Task<(int Count, string Summary)> GroupJoinSummaryAsync(
        ApplicationDbContext db, DateTimeOffset since, CancellationToken ct)
    {
        var rows = await (from m in db.GroupMemberships
                          join g in db.Groups on m.GroupId equals g.Id
                          where m.RequestedAt > since && m.Status == GroupMembershipStatus.Pending
                          orderby m.RequestedAt descending
                          select new { GroupName = g.Name, m.RequestedAt })
                          .Take(20)
                          .ToListAsync(ct).ConfigureAwait(false);
        if (rows.Count == 0) return (0, string.Empty);

        var sb = new System.Text.StringBuilder();
        sb.Append($"<p>You have {rows.Count} new group join request{(rows.Count == 1 ? "" : "s")}:</p><ul>");
        foreach (var r in rows)
            sb.Append($"<li>Requested to join <strong>{System.Web.HttpUtility.HtmlEncode(r.GroupName)}</strong></li>");
        sb.Append("</ul><p><a href=\"/admin/groups\">Review in admin</a></p>");
        return (rows.Count, sb.ToString());
    }

    private static TimeSpan WindowFor(AdminNotificationFrequency freq) => freq switch
    {
        AdminNotificationFrequency.Every30Minutes => TimeSpan.FromMinutes(30),
        AdminNotificationFrequency.Hourly => TimeSpan.FromHours(1),
        AdminNotificationFrequency.Daily => TimeSpan.FromHours(24),
        _ => TimeSpan.FromDays(365),
    };
}
