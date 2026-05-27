using CredoCms.Application.Common;
using CredoCms.Application.Email;
using CredoCms.Application.RealTime;
using CredoCms.Domain.Email;
using Moq;

namespace CredoCms.Application.Tests.Email;

public sealed class EmailBroadcastServiceTests
{
    private static (
        EmailBroadcastService Sut,
        Mock<IEmailBroadcastRepository> Broadcasts,
        Mock<IEmailBroadcastRecipientRepository> Recipients,
        Mock<IRecipientResolver> Resolver,
        Mock<IEmailService> Email,
        Mock<IRealtimeNotifier> Notifier) MakeSut()
    {
        var broadcasts = new Mock<IEmailBroadcastRepository>();
        var recipients = new Mock<IEmailBroadcastRecipientRepository>();
        var resolver = new Mock<IRecipientResolver>();
        var email = new Mock<IEmailService>();
        var notifier = new Mock<IRealtimeNotifier>();
        var audit = new Mock<IAuditLogger>();

        var sut = new EmailBroadcastService(
            broadcasts.Object, recipients.Object, resolver.Object,
            email.Object, notifier.Object, audit.Object);
        return (sut, broadcasts, recipients, resolver, email, notifier);
    }

    private static BroadcastDraftInput Input(BroadcastTargetMode mode = BroadcastTargetMode.AllMembers, IReadOnlyCollection<Guid>? groups = null) =>
        new(Subject: "Hi", Body: "<p>Hi</p>", PlainTextBody: "Hi",
            TargetMode: mode, TargetGroupIds: groups, Category: EmailCategory.Broadcast);

    [Fact]
    public async Task CreateDraft_persists_with_status_Draft()
    {
        var (sut, broadcasts, _, _, _, _) = MakeSut();

        var b = await sut.CreateDraftAsync(Input());

        b.Status.Should().Be(BroadcastStatus.Draft);
        b.Subject.Should().Be("Hi");
        broadcasts.Verify(r => r.AddAsync(It.IsAny<EmailBroadcast>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Schedule_rejects_past_send_time()
    {
        var (sut, broadcasts, _, _, _, _) = MakeSut();
        var b = new EmailBroadcast { Id = Guid.NewGuid(), Status = BroadcastStatus.Draft };
        broadcasts.Setup(r => r.GetAsync(b.Id, It.IsAny<CancellationToken>())).ReturnsAsync(b);

        await FluentActions.Invoking(() => sut.ScheduleAsync(b.Id, DateTimeOffset.UtcNow.AddMinutes(-5)))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Cancel_rejects_when_already_sent()
    {
        var (sut, broadcasts, _, _, _, _) = MakeSut();
        var b = new EmailBroadcast { Id = Guid.NewGuid(), Status = BroadcastStatus.Sent };
        broadcasts.Setup(r => r.GetAsync(b.Id, It.IsAny<CancellationToken>())).ReturnsAsync(b);

        await FluentActions.Invoking(() => sut.CancelAsync(b.Id))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task ExecuteSend_with_zero_recipients_marks_sent()
    {
        var (sut, broadcasts, recipients, resolver, email, _) = MakeSut();
        var b = new EmailBroadcast { Id = Guid.NewGuid(), Status = BroadcastStatus.Sending, TargetMode = BroadcastTargetMode.AllMembers };
        broadcasts.Setup(r => r.GetAsync(b.Id, It.IsAny<CancellationToken>())).ReturnsAsync(b);
        resolver.Setup(r => r.ResolveAsync(It.IsAny<BroadcastTargetMode>(), It.IsAny<IReadOnlyCollection<Guid>?>(), It.IsAny<EmailCategory>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<EmailRecipient>());

        await sut.ExecuteSendAsync(b.Id);

        b.Status.Should().Be(BroadcastStatus.Sent);
        b.RecipientCountAtSend.Should().Be(0);
        recipients.Verify(r => r.BulkInsertAsync(It.IsAny<IReadOnlyCollection<EmailBroadcastRecipient>>(), It.IsAny<CancellationToken>()), Times.Never);
        email.Verify(e => e.SendBroadcastAsync(It.IsAny<BroadcastEmailMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteSend_persists_recipients_dispatches_and_marks_sent()
    {
        var (sut, broadcasts, recipients, resolver, email, notifier) = MakeSut();
        var b = new EmailBroadcast { Id = Guid.NewGuid(), Status = BroadcastStatus.Sending };
        broadcasts.Setup(r => r.GetAsync(b.Id, It.IsAny<CancellationToken>())).ReturnsAsync(b);
        var resolved = new[]
        {
            new EmailRecipient("a@x.org", "A", null),
            new EmailRecipient("b@x.org", "B", null),
        };
        resolver.Setup(r => r.ResolveAsync(It.IsAny<BroadcastTargetMode>(), It.IsAny<IReadOnlyCollection<Guid>?>(), It.IsAny<EmailCategory>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(resolved);
        email.Setup(e => e.SendBroadcastAsync(It.IsAny<BroadcastEmailMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BroadcastSendResult(new[]
            {
                new RecipientSendResult(null, "a@x.org", true, "batch-1", null),
                new RecipientSendResult(null, "b@x.org", true, "batch-1", null),
            }));

        await sut.ExecuteSendAsync(b.Id);

        b.Status.Should().Be(BroadcastStatus.Sent);
        b.RecipientCountAtSend.Should().Be(2);
        recipients.Verify(r => r.BulkInsertAsync(
            It.Is<IReadOnlyCollection<EmailBroadcastRecipient>>(c => c.Count == 2),
            It.IsAny<CancellationToken>()), Times.Once);
        notifier.Verify(n => n.NotifyBroadcastLifecycleAsync(
            It.Is<BroadcastLifecycleMessage>(m => m.Kind == "BroadcastSendCompleted"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteSend_marks_failed_recipients_when_provider_partial()
    {
        var (sut, broadcasts, recipients, resolver, email, _) = MakeSut();
        var b = new EmailBroadcast { Id = Guid.NewGuid(), Status = BroadcastStatus.Sending };
        broadcasts.Setup(r => r.GetAsync(b.Id, It.IsAny<CancellationToken>())).ReturnsAsync(b);
        resolver.Setup(r => r.ResolveAsync(It.IsAny<BroadcastTargetMode>(), It.IsAny<IReadOnlyCollection<Guid>?>(), It.IsAny<EmailCategory>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new EmailRecipient("ok@x.org", "OK", null), new EmailRecipient("bad@x.org", "BAD", null) });
        email.Setup(e => e.SendBroadcastAsync(It.IsAny<BroadcastEmailMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BroadcastSendResult(new[]
            {
                new RecipientSendResult(null, "ok@x.org", true, "batch", null),
                new RecipientSendResult(null, "bad@x.org", false, null, "rejected"),
            }));
        var updated = new List<EmailBroadcastRecipient>();
        recipients.Setup(r => r.UpdateAsync(It.IsAny<EmailBroadcastRecipient>(), It.IsAny<CancellationToken>()))
            .Callback<EmailBroadcastRecipient, CancellationToken>((r, _) => updated.Add(r))
            .Returns(Task.CompletedTask);

        await sut.ExecuteSendAsync(b.Id);

        updated.Should().Contain(r => r.EmailAddressSnapshot == "bad@x.org" && r.Status == RecipientStatus.Failed);
        updated.Should().Contain(r => r.EmailAddressSnapshot == "ok@x.org" && r.SendGridMessageId == "batch");
    }
}
