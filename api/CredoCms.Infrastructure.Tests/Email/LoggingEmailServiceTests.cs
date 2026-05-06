using CredoCms.Application.Common;
using CredoCms.Application.SiteSettingsManagement;
using CredoCms.Domain.Email;
using CredoCms.Domain.Settings;
using CredoCms.Infrastructure.Email;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace CredoCms.Infrastructure.Tests.Email;

public sealed class LoggingEmailServiceTests
{
    private static (LoggingEmailService Sut, Mock<ISiteSettingsRepository> Settings) MakeSut(bool emailEnabled)
    {
        var settings = new Mock<ISiteSettingsRepository>();
        settings.Setup(s => s.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SiteSettings { EmailEnabled = emailEnabled });
        return (new LoggingEmailService(NullLogger<LoggingEmailService>.Instance, settings.Object), settings);
    }

    private static EmailMessage SampleMessage(EmailCategory category = EmailCategory.Transactional) =>
        new(
            ToAddress: "test@example.org",
            ToName: "Tester",
            Subject: "Subject",
            HtmlBody: "<p>Hello</p>",
            PlainTextBody: "Hello",
            UserId: null,
            Category: category);

    [Fact]
    public async Task IsConfigured_always_true()
    {
        var (sut, _) = MakeSut(emailEnabled: true);
        (await sut.IsConfiguredAsync()).Should().BeTrue();
    }

    [Fact]
    public async Task SendTransactional_succeeds_when_enabled()
    {
        var (sut, _) = MakeSut(emailEnabled: true);
        await sut.SendTransactionalAsync(SampleMessage());
        // No exception = success.
    }

    [Fact]
    public async Task SendTransactional_skips_silently_when_email_disabled()
    {
        var (sut, settings) = MakeSut(emailEnabled: false);
        await sut.SendTransactionalAsync(SampleMessage());
        settings.Verify(s => s.GetAsync(It.IsAny<CancellationToken>()), Times.Once);
        // The method returned cleanly — short-circuit honored.
    }

    [Fact]
    public async Task SendBroadcast_skips_silently_when_email_disabled()
    {
        var (sut, _) = MakeSut(emailEnabled: false);
        var msg = new BroadcastEmailMessage(
            Subject: "Hello",
            HtmlBody: "<p>Hello</p>",
            PlainTextBody: "Hello",
            Recipients: new[] { new EmailRecipient("a@example.org", "A", null) },
            BroadcastId: Guid.NewGuid(),
            Category: EmailCategory.Broadcast);
        await sut.SendBroadcastAsync(msg);
    }

    [Fact]
    public async Task SendTransactional_throws_on_null_message()
    {
        var (sut, _) = MakeSut(emailEnabled: true);
        await FluentActions.Invoking(() => sut.SendTransactionalAsync(null!))
            .Should().ThrowAsync<ArgumentNullException>();
    }
}
