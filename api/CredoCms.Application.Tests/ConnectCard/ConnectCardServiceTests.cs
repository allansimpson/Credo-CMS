using CredoCms.Application.Common;
using CredoCms.Application.ConnectCard;
using CredoCms.Application.RealTime;
using CredoCms.Application.SiteSettingsManagement;
using CredoCms.Domain.Common;
using CredoCms.Domain.ConnectCard;
using CredoCms.Domain.Settings;
using Moq;

namespace CredoCms.Application.Tests.ConnectCard;

/// <summary>
/// Anti-bot + happy-path coverage for ConnectCardService:
///   • Honeypot rejects (filled value → fail)
///   • &lt; 5s time-to-submit rejects
///   • Turnstile failure rejects
///   • Validation enforces "email or phone required"
///   • Happy path persists, sends ack email, emits SignalR event
/// </summary>
public sealed class ConnectCardServiceTests
{
    private static (
        ConnectCardService sut,
        Mock<IConnectCardRepository> repo,
        Mock<ITurnstileValidationService> turnstile,
        Mock<IEmailService> email,
        Mock<IRealtimeNotifier> notifier)
        MakeSut(string[]? roles = null)
    {
        var repo = new Mock<IConnectCardRepository>();
        var turnstile = new Mock<ITurnstileValidationService>();
        turnstile.Setup(x => x.ValidateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var email = new Mock<IEmailService>();

        var settings = new Mock<ISiteSettingsRepository>();
        settings.Setup(x => x.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SiteSettings { ChurchName = "Grace Church" });

        var currentUser = new Mock<ICurrentUserService>();
        currentUser.SetupGet(x => x.UserId).Returns(SystemConstants.SystemUserId);
        currentUser.SetupGet(x => x.Roles).Returns(roles ?? Array.Empty<string>());

        var notifier = new Mock<IRealtimeNotifier>();
        var audit = new Mock<IAuditLogger>();

        var sut = new ConnectCardService(
            repo.Object, turnstile.Object, email.Object, settings.Object,
            currentUser.Object, notifier.Object, audit.Object,
            new SubmitConnectCardRequestValidator());
        return (sut, repo, turnstile, email, notifier);
    }

    private static SubmitConnectCardRequest ValidRequest(
        DateTimeOffset? clientLoadedAt = null,
        string? honeypot = null,
        string? token = "valid",
        string? email = "alice@example.com",
        string? phone = null) => new(
            Name: "Alice Adams",
            Email: email,
            Phone: phone,
            IsFirstTimeVisitor: true,
            ServiceDate: null,
            HowDidYouHear: "Friend invited me",
            Comments: null,
            Interests: null,
            HoneypotValue: honeypot,
            ClientLoadedAt: clientLoadedAt ?? DateTimeOffset.UtcNow.AddSeconds(-10),
            TurnstileToken: token);

    [Fact]
    public async Task Honeypot_filled_is_rejected()
    {
        var (sut, repo, _, _, _) = MakeSut();
        var result = await sut.SubmitAsync(ValidRequest(honeypot: "spam"), remoteIp: "127.0.0.1");
        result.Succeeded.Should().BeFalse();
        repo.Verify(x => x.AddAsync(It.IsAny<ConnectCardSubmission>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Time_to_submit_under_5_seconds_is_rejected()
    {
        var (sut, repo, _, _, _) = MakeSut();
        var result = await sut.SubmitAsync(
            ValidRequest(clientLoadedAt: DateTimeOffset.UtcNow.AddSeconds(-2)),
            remoteIp: "127.0.0.1");
        result.Succeeded.Should().BeFalse();
        repo.Verify(x => x.AddAsync(It.IsAny<ConnectCardSubmission>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Turnstile_failure_is_rejected()
    {
        var (sut, repo, turnstile, _, _) = MakeSut();
        turnstile.Setup(x => x.ValidateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await sut.SubmitAsync(ValidRequest(), remoteIp: "127.0.0.1");

        result.Succeeded.Should().BeFalse();
        result.Errors[0].Should().Contain("verify");
        repo.Verify(x => x.AddAsync(It.IsAny<ConnectCardSubmission>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task At_least_one_of_email_or_phone_is_required()
    {
        var (sut, repo, _, _, _) = MakeSut();
        var result = await sut.SubmitAsync(
            ValidRequest(email: null, phone: null),
            remoteIp: "127.0.0.1");
        result.Succeeded.Should().BeFalse();
        result.Errors[0].Should().Contain("email address or a phone");
        repo.Verify(x => x.AddAsync(It.IsAny<ConnectCardSubmission>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Happy_path_persists_and_sends_ack_and_emits_signalr()
    {
        var (sut, repo, _, email, notifier) = MakeSut();

        var result = await sut.SubmitAsync(ValidRequest(), remoteIp: "127.0.0.1");

        result.Succeeded.Should().BeTrue();
        repo.Verify(x => x.AddAsync(
            It.Is<ConnectCardSubmission>(s => s.Name == "Alice Adams"
                && s.Email == "alice@example.com"
                && s.Status == ConnectCardStatus.New
                && s.IpAddressHash != null),
            It.IsAny<CancellationToken>()), Times.Once);
        email.Verify(x => x.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()), Times.Once);
        notifier.Verify(x => x.NotifyConnectCardSubmittedAsync(
            It.Is<ConnectCardSummaryMessage>(m => m.Name == "Alice Adams"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Phone_only_submission_skips_email_send()
    {
        var (sut, _, _, email, _) = MakeSut();

        var result = await sut.SubmitAsync(
            ValidRequest(email: null, phone: "555-0100"),
            remoteIp: "127.0.0.1");

        result.Succeeded.Should().BeTrue();
        email.Verify(x => x.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ListAdmin_returns_empty_for_non_admin()
    {
        var (sut, _, _, _, _) = MakeSut(); // no roles
        var result = await sut.ListAdminAsync(new AdminConnectCardListQuery());
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateStatus_returns_null_for_non_admin()
    {
        var (sut, _, _, _, _) = MakeSut(); // no roles
        var result = await sut.UpdateStatusAsync(Guid.NewGuid(),
            new UpdateStatusRequest(ConnectCardStatus.FollowUpNeeded));
        result.Should().BeNull();
    }
}
