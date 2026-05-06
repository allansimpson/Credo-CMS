using CredoCms.Application.Common;
using CredoCms.Application.Volunteers;
using CredoCms.Domain.Email;
using CredoCms.Domain.Identity;
using CredoCms.Domain.Volunteers;
using CredoCms.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CredoCms.Infrastructure.BackgroundServices;

/// <summary>
/// Phase 5 R13. Daily-cadence reminder for volunteer commitments due
/// 1–2 days out. Sends a transactional email per signup, marks
/// <see cref="EventVolunteerSignup.ReminderEmailSentAt"/> so dupes don't
/// fire on the next tick. Cancels and past-date signups are skipped by
/// the repo's filter.
/// </summary>
public sealed class EventVolunteerReminderService : BackgroundService
{
    private static readonly TimeSpan TickInterval = TimeSpan.FromHours(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EventVolunteerReminderService> _logger;

    public EventVolunteerReminderService(IServiceScopeFactory scopeFactory, ILogger<EventVolunteerReminderService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("EventVolunteerReminderService started, tick={Tick}", TickInterval);
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await TickAsync(stoppingToken).ConfigureAwait(false); }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "EventVolunteerReminderService tick threw");
            }
            await Task.Delay(TickInterval, stoppingToken).ConfigureAwait(false);
        }
    }

    private async Task TickAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var signups = scope.ServiceProvider.GetRequiredService<IEventVolunteerSignupRepository>();
        var roles = scope.ServiceProvider.GetRequiredService<IEventVolunteerRoleRepository>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var email = scope.ServiceProvider.GetRequiredService<IEmailService>();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var due = await signups.ListDueForReminderAsync(today, ct).ConfigureAwait(false);

        foreach (var s in due)
        {
            try
            {
                var role = await roles.GetAsync(s.EventVolunteerRoleId, ct).ConfigureAwait(false);
                var user = await users.FindByIdAsync(s.UserId.ToString());
                if (role is null || user is null || string.IsNullOrWhiteSpace(user.Email)) continue;
                var ev = await db.Events.AsNoTracking()
                    .FirstOrDefaultAsync(e => e.Id == role.EventId, ct).ConfigureAwait(false);

                var msg = new EmailMessage(
                    ToAddress: user.Email,
                    ToName: user.DisplayName,
                    Subject: $"Volunteer reminder: {role.RoleName} on {s.OccurrenceDate:MMM d}",
                    HtmlBody: BuildHtml(user.FirstName, role.RoleName, ev?.Title ?? "the event", s.OccurrenceDate),
                    PlainTextBody: BuildText(user.FirstName, role.RoleName, ev?.Title ?? "the event", s.OccurrenceDate),
                    UserId: user.Id,
                    Category: EmailCategory.Transactional);
                await email.SendTransactionalAsync(msg, ct).ConfigureAwait(false);
                s.ReminderEmailSentAt = DateTimeOffset.UtcNow;
                await signups.UpdateAsync(s, ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Volunteer reminder send failed for signup {Id}", s.Id);
            }
        }
    }

    private static string BuildHtml(string firstName, string role, string eventTitle, DateOnly date) =>
        $"<p>Hi {System.Web.HttpUtility.HtmlEncode(firstName)},</p>"
        + $"<p>This is a reminder that you signed up to volunteer as <strong>{System.Web.HttpUtility.HtmlEncode(role)}</strong>"
        + $" for <strong>{System.Web.HttpUtility.HtmlEncode(eventTitle)}</strong> on {date:dddd, MMMM d}.</p>"
        + "<p>Thanks for serving!</p>";

    private static string BuildText(string firstName, string role, string eventTitle, DateOnly date) =>
        $"Hi {firstName},\n\nThis is a reminder that you signed up to volunteer as {role}"
        + $" for {eventTitle} on {date:dddd, MMMM d}.\n\nThanks for serving!\n";
}
