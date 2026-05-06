using CredoCms.Application.Email;
using CredoCms.Application.RealTime;
using CredoCms.Domain.Email;
using CredoCms.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace CredoCms.Application.Tests.Email;

public sealed class SendGridWebhookEventProcessorTests
{
    private static (
        SendGridWebhookEventProcessor Sut,
        Mock<IWebhookEventLogRepository> Log,
        Mock<IEmailSuppressionService> Suppression,
        Mock<IEmailBroadcastRecipientRepository> Recipients,
        Mock<IEmailBroadcastRepository> Broadcasts,
        Mock<IRealtimeNotifier> Notifier,
        Mock<UserManager<ApplicationUser>> Users) MakeSut()
    {
        var log = new Mock<IWebhookEventLogRepository>();
        log.Setup(l => l.ExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        log.Setup(l => l.AddAsync(It.IsAny<WebhookEventLog>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var suppression = new Mock<IEmailSuppressionService>();
        suppression.Setup(s => s.AddAsync(It.IsAny<string>(), It.IsAny<SuppressionType>(),
            It.IsAny<SuppressionSource>(), It.IsAny<string?>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var recipients = new Mock<IEmailBroadcastRecipientRepository>();
        var broadcasts = new Mock<IEmailBroadcastRepository>();
        broadcasts.Setup(b => b.IncrementStatsAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, int d, int b, int c, int o, CancellationToken _) =>
                new EmailBroadcast { Id = id, DeliveredCount = d, BouncedCount = b, ComplaintCount = c, OpenCount = o });
        var notifier = new Mock<IRealtimeNotifier>();

        var store = new Mock<IUserStore<ApplicationUser>>();
        var users = new Mock<UserManager<ApplicationUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        users.Setup(u => u.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser?)null);

        var sut = new SendGridWebhookEventProcessor(
            log.Object, suppression.Object, recipients.Object, broadcasts.Object,
            notifier.Object, users.Object, NullLogger<SendGridWebhookEventProcessor>.Instance);
        return (sut, log, suppression, recipients, broadcasts, notifier, users);
    }

    [Fact]
    public async Task Skips_duplicate_event_ids()
    {
        var (sut, log, _, _, _, _, _) = MakeSut();
        log.Setup(l => l.ExistsAsync("dup", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var applied = await sut.ProcessAsync(new[]
        {
            new SendGridWebhookEvent("delivered", "dup", "msg-1", "x@y.org", 0),
        });

        applied.Should().Be(0);
        log.Verify(l => l.AddAsync(It.IsAny<WebhookEventLog>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Hard_bounce_adds_to_suppression()
    {
        var (sut, _, suppression, recipients, _, _, _) = MakeSut();
        recipients.Setup(r => r.GetBySendGridMessageIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EmailBroadcastRecipient?)null);

        await sut.ProcessAsync(new[]
        {
            new SendGridWebhookEvent("bounce", "ev-1", "msg-1", "bad@example.org", 0, Reason: "550 5.1.1", Type: "bounce"),
        });

        suppression.Verify(s => s.AddAsync("bad@example.org", SuppressionType.HardBounce,
            SuppressionSource.SendGridWebhook, "550 5.1.1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Soft_bounce_does_not_add_to_suppression()
    {
        var (sut, _, suppression, recipients, _, _, _) = MakeSut();
        recipients.Setup(r => r.GetBySendGridMessageIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EmailBroadcastRecipient?)null);

        await sut.ProcessAsync(new[]
        {
            new SendGridWebhookEvent("bounce", "ev-1", "msg-1", "soft@example.org", 0, Reason: "421 Try later", Type: "blocked"),
        });

        suppression.Verify(s => s.AddAsync(It.IsAny<string>(), It.IsAny<SuppressionType>(),
            It.IsAny<SuppressionSource>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Spam_report_disables_user_preferences_and_suppresses()
    {
        var (sut, _, suppression, _, _, _, users) = MakeSut();
        var user = new ApplicationUser
        {
            Email = "spammed@example.org",
            ReceiveNewsEmails = true,
            ReceiveBroadcastEmails = true,
            ReceiveBlogEmails = true,
            ReceiveGroupEmailsGlobal = true,
        };
        users.Setup(u => u.FindByEmailAsync("spammed@example.org")).ReturnsAsync(user);
        users.Setup(u => u.UpdateAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(IdentityResult.Success);

        await sut.ProcessAsync(new[]
        {
            new SendGridWebhookEvent("spamreport", "ev-1", "msg-1", "spammed@example.org", 0),
        });

        suppression.Verify(s => s.AddAsync("spammed@example.org", SuppressionType.SpamComplaint,
            SuppressionSource.SendGridWebhook, It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
        user.ReceiveNewsEmails.Should().BeFalse();
        user.ReceiveBroadcastEmails.Should().BeFalse();
        user.ReceiveBlogEmails.Should().BeFalse();
        user.ReceiveGroupEmailsGlobal.Should().BeFalse();
    }

    [Fact]
    public async Task Delivered_updates_recipient_and_broadcasts_stats_event()
    {
        var (sut, _, _, recipients, broadcasts, notifier, _) = MakeSut();
        var broadcastId = Guid.NewGuid();
        recipients.Setup(r => r.GetBySendGridMessageIdAsync("msg-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EmailBroadcastRecipient { BroadcastId = broadcastId, EmailAddressSnapshot = "a@x.org", DisplayNameSnapshot = "A", Status = RecipientStatus.Pending });

        await sut.ProcessAsync(new[]
        {
            new SendGridWebhookEvent("delivered", "ev-1", "msg-1", "a@x.org", 0),
        });

        recipients.Verify(r => r.UpdateAsync(It.Is<EmailBroadcastRecipient>(rr => rr.Status == RecipientStatus.Delivered),
            It.IsAny<CancellationToken>()), Times.Once);
        broadcasts.Verify(b => b.IncrementStatsAsync(broadcastId, 1, 0, 0, 0, It.IsAny<CancellationToken>()), Times.Once);
        notifier.Verify(n => n.NotifyBroadcastLifecycleAsync(
            It.Is<BroadcastLifecycleMessage>(m => m.Kind == "BroadcastStatsUpdated" && m.BroadcastId == broadcastId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Aggregates_multiple_events_into_one_stats_call()
    {
        var (sut, _, _, recipients, broadcasts, _, _) = MakeSut();
        var broadcastId = Guid.NewGuid();
        recipients.Setup(r => r.GetBySendGridMessageIdAsync("msg-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EmailBroadcastRecipient { BroadcastId = broadcastId, EmailAddressSnapshot = "a@x.org", DisplayNameSnapshot = "A" });
        recipients.Setup(r => r.GetBySendGridMessageIdAsync("msg-2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EmailBroadcastRecipient { BroadcastId = broadcastId, EmailAddressSnapshot = "b@x.org", DisplayNameSnapshot = "B" });

        await sut.ProcessAsync(new[]
        {
            new SendGridWebhookEvent("delivered", "ev-1", "msg-1", "a@x.org", 0),
            new SendGridWebhookEvent("delivered", "ev-2", "msg-2", "b@x.org", 0),
            new SendGridWebhookEvent("open", "ev-3", "msg-1", "a@x.org", 0),
        });

        broadcasts.Verify(b => b.IncrementStatsAsync(broadcastId, 2, 0, 0, 1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Ignores_events_with_blank_sg_event_id()
    {
        var (sut, log, _, _, _, _, _) = MakeSut();

        var applied = await sut.ProcessAsync(new[]
        {
            new SendGridWebhookEvent("delivered", "", "msg-1", "x@y.org", 0),
        });

        applied.Should().Be(0);
        log.Verify(l => l.AddAsync(It.IsAny<WebhookEventLog>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
