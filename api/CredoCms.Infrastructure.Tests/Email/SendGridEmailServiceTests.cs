using System.Net;
using System.Net.Http;
using CredoCms.Application.Common;
using CredoCms.Application.SiteSettingsManagement;
using CredoCms.Domain.Email;
using CredoCms.Domain.Settings;
using CredoCms.Infrastructure.Email;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace CredoCms.Infrastructure.Tests.Email;

public sealed class SendGridEmailServiceTests
{
    private static SiteSettings DefaultSettings(bool emailEnabled = true, string? apiKey = "SG.test", string? testRecipient = null) => new()
    {
        EmailProvider = EmailProvider.SendGrid,
        EmailFromAddress = "noreply@example.org",
        EmailFromName = "Church Test",
        EmailReplyToAddress = "office@example.org",
        SendGridApiKey = apiKey,
        EmailEnabled = emailEnabled,
        TestEmailRecipient = testRecipient,
    };

    private sealed class CapturingFactory : ISendGridClientFactory
    {
        public List<string> ApiKeysSeen { get; } = new();
        public Mock<ISendGridClient> Client { get; } = new();
        public ISendGridClient Create(string apiKey)
        {
            ApiKeysSeen.Add(apiKey);
            return Client.Object;
        }
    }

    private static (SendGridEmailService Sut, CapturingFactory Factory, Mock<ISiteSettingsRepository> Settings)
        MakeSut(SiteSettings settings, Response sendResponse)
    {
        var factory = new CapturingFactory();
        factory.Client
            .Setup(c => c.SendEmailAsync(It.IsAny<SendGridMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sendResponse);

        var settingsMock = new Mock<ISiteSettingsRepository>();
        settingsMock.Setup(s => s.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(settings);

        var sut = new SendGridEmailService(factory, settingsMock.Object, NullLogger<SendGridEmailService>.Instance);
        return (sut, factory, settingsMock);
    }

    private static Response MakeResponse(HttpStatusCode status, string? messageId = null)
    {
        var headers = new HttpResponseMessage().Headers;
        if (messageId is not null) headers.Add("X-Message-Id", messageId);
        return new Response(status, new StringContent(string.Empty), headers);
    }

    private static EmailMessage TransactionalSample() => new(
        ToAddress: "user@example.org",
        ToName: "User",
        Subject: "Subject",
        HtmlBody: "<p>Hello</p>",
        PlainTextBody: "Hello",
        UserId: Guid.NewGuid(),
        Category: EmailCategory.Transactional);

    [Fact]
    public async Task IsConfigured_returns_false_when_email_disabled()
    {
        var (sut, _, _) = MakeSut(DefaultSettings(emailEnabled: false), MakeResponse(HttpStatusCode.Accepted));
        (await sut.IsConfiguredAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task IsConfigured_returns_false_when_api_key_missing()
    {
        var (sut, _, _) = MakeSut(DefaultSettings(apiKey: null), MakeResponse(HttpStatusCode.Accepted));
        (await sut.IsConfiguredAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task IsConfigured_true_when_provider_and_key_set_and_enabled()
    {
        var (sut, _, _) = MakeSut(DefaultSettings(), MakeResponse(HttpStatusCode.Accepted));
        (await sut.IsConfiguredAsync()).Should().BeTrue();
    }

    [Fact]
    public async Task SendTransactional_skips_when_email_disabled()
    {
        var (sut, factory, _) = MakeSut(DefaultSettings(emailEnabled: false), MakeResponse(HttpStatusCode.Accepted));
        await sut.SendTransactionalAsync(TransactionalSample());
        factory.Client.Verify(c => c.SendEmailAsync(It.IsAny<SendGridMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SendTransactional_uses_configured_api_key()
    {
        var (sut, factory, _) = MakeSut(DefaultSettings(apiKey: "SG.live-key"), MakeResponse(HttpStatusCode.Accepted));
        await sut.SendTransactionalAsync(TransactionalSample());
        factory.ApiKeysSeen.Should().BeEquivalentTo(new[] { "SG.live-key" });
    }

    [Fact]
    public async Task SendTransactional_throws_on_non_2xx_response()
    {
        var (sut, _, _) = MakeSut(DefaultSettings(), MakeResponse(HttpStatusCode.Forbidden));
        await FluentActions.Invoking(() => sut.SendTransactionalAsync(TransactionalSample()))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task SendTransactional_redirects_to_test_recipient_when_configured()
    {
        SendGridMessage? captured = null;
        var settings = DefaultSettings(testRecipient: "stage@example.org");
        var (sut, factory, _) = MakeSut(settings, MakeResponse(HttpStatusCode.Accepted));
        factory.Client
            .Setup(c => c.SendEmailAsync(It.IsAny<SendGridMessage>(), It.IsAny<CancellationToken>()))
            .Callback<SendGridMessage, CancellationToken>((m, _) => captured = m)
            .ReturnsAsync(MakeResponse(HttpStatusCode.Accepted));

        await sut.SendTransactionalAsync(TransactionalSample());

        captured.Should().NotBeNull();
        captured!.Personalizations.Should().HaveCount(1);
        captured.Personalizations[0].Tos[0].Email.Should().Be("stage@example.org");
    }

    [Fact]
    public async Task SendBroadcast_returns_skipped_results_when_disabled()
    {
        var (sut, factory, _) = MakeSut(DefaultSettings(emailEnabled: false), MakeResponse(HttpStatusCode.Accepted));
        var broadcast = new BroadcastEmailMessage(
            "S", "<p>B</p>", "B",
            new[] { new EmailRecipient("a@x.org", "A", null), new EmailRecipient("b@x.org", "B", null) },
            Guid.NewGuid(),
            EmailCategory.Broadcast);

        var result = await sut.SendBroadcastAsync(broadcast);

        result.Recipients.Should().HaveCount(2);
        result.Recipients.Should().AllSatisfy(r => r.Success.Should().BeTrue());
        result.Recipients.Should().AllSatisfy(r => r.SendGridMessageId.Should().BeNull());
        factory.Client.Verify(c => c.SendEmailAsync(It.IsAny<SendGridMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SendBroadcast_chunks_at_500_recipients()
    {
        var (sut, factory, _) = MakeSut(DefaultSettings(), MakeResponse(HttpStatusCode.Accepted, "batch-id-123"));
        var recipients = Enumerable.Range(0, 1200)
            .Select(i => new EmailRecipient($"u{i}@example.org", $"U{i}", null))
            .ToList();
        var broadcast = new BroadcastEmailMessage("S", "<p>B</p>", "B", recipients, Guid.NewGuid(), EmailCategory.Broadcast);

        var result = await sut.SendBroadcastAsync(broadcast);

        // 1200 → chunks of 500, 500, 200 → 3 HTTP calls.
        factory.Client.Verify(
            c => c.SendEmailAsync(It.IsAny<SendGridMessage>(), It.IsAny<CancellationToken>()),
            Times.Exactly(3));
        result.Recipients.Should().HaveCount(1200);
        result.Recipients.Should().AllSatisfy(r => r.Success.Should().BeTrue());
        result.Recipients.Should().AllSatisfy(r => r.SendGridMessageId.Should().Be("batch-id-123"));
    }

    [Fact]
    public async Task SendBroadcast_marks_chunk_failed_on_4xx_response()
    {
        var (sut, _, _) = MakeSut(DefaultSettings(), MakeResponse(HttpStatusCode.BadRequest));
        var broadcast = new BroadcastEmailMessage(
            "S", "<p>B</p>", "B",
            new[] { new EmailRecipient("a@x.org", "A", null) },
            Guid.NewGuid(),
            EmailCategory.Broadcast);

        var result = await sut.SendBroadcastAsync(broadcast);

        result.Recipients.Should().HaveCount(1);
        result.Recipients[0].Success.Should().BeFalse();
        result.Recipients[0].SendGridMessageId.Should().BeNull();
        result.Recipients[0].ErrorMessage.Should().Contain("400");
    }

    [Fact]
    public async Task SendBroadcast_substitutes_merge_fields_per_recipient()
    {
        SendGridMessage? captured = null;
        var (sut, factory, _) = MakeSut(DefaultSettings(), MakeResponse(HttpStatusCode.Accepted, "abc"));
        factory.Client
            .Setup(c => c.SendEmailAsync(It.IsAny<SendGridMessage>(), It.IsAny<CancellationToken>()))
            .Callback<SendGridMessage, CancellationToken>((m, _) => captured = m)
            .ReturnsAsync(MakeResponse(HttpStatusCode.Accepted, "abc"));

        var recipients = new[]
        {
            new EmailRecipient("a@x.org", "A", null,
                new Dictionary<string, string> { ["firstName"] = "Alice" }),
            new EmailRecipient("b@x.org", "B", null,
                new Dictionary<string, string> { ["firstName"] = "Bob" }),
        };
        var broadcast = new BroadcastEmailMessage("Hi {{firstName}}", "<p>Hi {{firstName}}</p>", null,
            recipients, Guid.NewGuid(), EmailCategory.Broadcast);

        await sut.SendBroadcastAsync(broadcast);

        captured.Should().NotBeNull();
        captured!.Personalizations.Should().HaveCount(2);
        captured.Personalizations[0].Substitutions.Should().ContainKey("{{firstName}}").WhoseValue.Should().Be("Alice");
        captured.Personalizations[1].Substitutions.Should().ContainKey("{{firstName}}").WhoseValue.Should().Be("Bob");
    }

    [Fact]
    public async Task SendBroadcast_continues_remaining_chunks_after_one_chunk_fails()
    {
        var responses = new Queue<Response>(new[]
        {
            MakeResponse(HttpStatusCode.BadRequest),
            MakeResponse(HttpStatusCode.Accepted, "ok-chunk"),
        });
        var factory = new CapturingFactory();
        factory.Client
            .Setup(c => c.SendEmailAsync(It.IsAny<SendGridMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => responses.Dequeue());
        var settings = new Mock<ISiteSettingsRepository>();
        settings.Setup(s => s.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(DefaultSettings());
        var sut = new SendGridEmailService(factory, settings.Object, NullLogger<SendGridEmailService>.Instance);

        var recipients = Enumerable.Range(0, 600)
            .Select(i => new EmailRecipient($"u{i}@x.org", $"U{i}", null))
            .ToList();
        var broadcast = new BroadcastEmailMessage("S", "<p>B</p>", null, recipients, Guid.NewGuid(), EmailCategory.Broadcast);

        var result = await sut.SendBroadcastAsync(broadcast);

        result.Recipients.Should().HaveCount(600);
        result.Recipients.Take(500).Should().AllSatisfy(r => r.Success.Should().BeFalse());
        result.Recipients.Skip(500).Should().AllSatisfy(r => r.Success.Should().BeTrue());
        result.Recipients.Skip(500).Should().AllSatisfy(r => r.SendGridMessageId.Should().Be("ok-chunk"));
    }

    [Fact]
    public async Task SendTransactional_retries_once_on_5xx()
    {
        var responses = new Queue<Response>(new[]
        {
            MakeResponse(HttpStatusCode.InternalServerError),
            MakeResponse(HttpStatusCode.Accepted),
        });
        var factory = new CapturingFactory();
        factory.Client
            .Setup(c => c.SendEmailAsync(It.IsAny<SendGridMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => responses.Dequeue());
        var settings = new Mock<ISiteSettingsRepository>();
        settings.Setup(s => s.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(DefaultSettings());
        var sut = new SendGridEmailService(factory, settings.Object, NullLogger<SendGridEmailService>.Instance);

        await sut.SendTransactionalAsync(TransactionalSample());

        factory.Client.Verify(c => c.SendEmailAsync(It.IsAny<SendGridMessage>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }
}
