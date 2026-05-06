using CredoCms.Application.Common;
using CredoCms.Application.SiteSettingsManagement;
using CredoCms.Domain.Email;
using CredoCms.Domain.Settings;
using CredoCms.Infrastructure.Email;
using MailKit.Security;
using Microsoft.Extensions.Logging.Abstractions;
using MimeKit;
using Moq;

namespace CredoCms.Infrastructure.Tests.Email;

public sealed class SmtpEmailServiceTests
{
    private static SiteSettings DefaultSettings(
        bool emailEnabled = true,
        string? smtpHost = "smtp.example.org",
        int smtpPort = 587,
        bool smtpUseSsl = true,
        string? smtpUser = "user",
        string? smtpPass = "pass",
        string? testRecipient = null,
        string? replyTo = "office@example.org") => new()
    {
        EmailProvider = EmailProvider.Smtp,
        EmailFromAddress = "noreply@example.org",
        EmailFromName = "Church Test",
        EmailReplyToAddress = replyTo,
        SmtpHost = smtpHost,
        SmtpPort = smtpPort,
        SmtpUseSsl = smtpUseSsl,
        SmtpUsername = smtpUser,
        SmtpPassword = smtpPass,
        EmailEnabled = emailEnabled,
        TestEmailRecipient = testRecipient,
    };

    private sealed class CapturingFactory : IMailKitSmtpClientFactory
    {
        public Mock<IMailKitSmtpClient> Client { get; } = new();
        public List<MimeMessage> SentMessages { get; } = new();
        public string? ConnectedHost { get; set; }
        public int ConnectedPort { get; set; }
        public SecureSocketOptions ConnectedOptions { get; set; }
        public string? AuthenticatedUser { get; set; }

        public CapturingFactory()
        {
            Client.Setup(c => c.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<SecureSocketOptions>(), It.IsAny<CancellationToken>()))
                .Callback<string, int, SecureSocketOptions, CancellationToken>((h, p, o, _) =>
                {
                    ConnectedHost = h; ConnectedPort = p; ConnectedOptions = o;
                })
                .Returns(Task.CompletedTask);
            Client.Setup(c => c.AuthenticateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<string, string, CancellationToken>((u, _, _) => AuthenticatedUser = u)
                .Returns(Task.CompletedTask);
            Client.Setup(c => c.SendAsync(It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>()))
                .Callback<MimeMessage, CancellationToken>((m, _) => SentMessages.Add(m))
                .ReturnsAsync("smtp-id-1");
            Client.Setup(c => c.DisconnectAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
        }

        public IMailKitSmtpClient Create() => Client.Object;
    }

    private static (SmtpEmailService Sut, CapturingFactory Factory) MakeSut(SiteSettings settings)
    {
        var factory = new CapturingFactory();
        var settingsMock = new Mock<ISiteSettingsRepository>();
        settingsMock.Setup(s => s.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(settings);
        var sut = new SmtpEmailService(factory, settingsMock.Object, NullLogger<SmtpEmailService>.Instance);
        return (sut, factory);
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
    public async Task IsConfigured_returns_false_when_disabled_or_unconfigured()
    {
        var (a, _) = MakeSut(DefaultSettings(emailEnabled: false));
        (await a.IsConfiguredAsync()).Should().BeFalse();

        var (b, _) = MakeSut(DefaultSettings(smtpHost: null));
        (await b.IsConfiguredAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task IsConfigured_true_when_provider_set_host_set_and_enabled()
    {
        var (sut, _) = MakeSut(DefaultSettings());
        (await sut.IsConfiguredAsync()).Should().BeTrue();
    }

    [Fact]
    public async Task SendTransactional_skips_when_email_disabled()
    {
        var (sut, factory) = MakeSut(DefaultSettings(emailEnabled: false));
        await sut.SendTransactionalAsync(TransactionalSample());
        factory.SentMessages.Should().BeEmpty();
    }

    [Fact]
    public async Task SendTransactional_connects_authenticates_sends_disconnects()
    {
        var (sut, factory) = MakeSut(DefaultSettings());
        await sut.SendTransactionalAsync(TransactionalSample());

        factory.ConnectedHost.Should().Be("smtp.example.org");
        factory.ConnectedPort.Should().Be(587);
        factory.ConnectedOptions.Should().Be(SecureSocketOptions.StartTls);
        factory.AuthenticatedUser.Should().Be("user");
        factory.SentMessages.Should().HaveCount(1);
        factory.Client.Verify(c => c.DisconnectAsync(true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendTransactional_skips_authenticate_when_no_username()
    {
        var (sut, factory) = MakeSut(DefaultSettings(smtpUser: null));
        await sut.SendTransactionalAsync(TransactionalSample());
        factory.Client.Verify(c => c.AuthenticateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(465, true, SecureSocketOptions.SslOnConnect)]
    [InlineData(587, true, SecureSocketOptions.StartTls)]
    [InlineData(25, true, SecureSocketOptions.Auto)]
    [InlineData(587, false, SecureSocketOptions.None)]
    public async Task SendTransactional_resolves_secure_options_from_port_and_ssl(int port, bool useSsl, SecureSocketOptions expected)
    {
        var (sut, factory) = MakeSut(DefaultSettings(smtpPort: port, smtpUseSsl: useSsl));
        await sut.SendTransactionalAsync(TransactionalSample());
        factory.ConnectedOptions.Should().Be(expected);
    }

    [Fact]
    public async Task SendTransactional_sets_reply_to_when_configured()
    {
        var (sut, factory) = MakeSut(DefaultSettings(replyTo: "office@example.org"));
        await sut.SendTransactionalAsync(TransactionalSample());
        var msg = factory.SentMessages.Single();
        msg.ReplyTo.Mailboxes.Should().ContainSingle().Which.Address.Should().Be("office@example.org");
    }

    [Fact]
    public async Task SendTransactional_omits_reply_to_when_blank()
    {
        var (sut, factory) = MakeSut(DefaultSettings(replyTo: null));
        await sut.SendTransactionalAsync(TransactionalSample());
        var msg = factory.SentMessages.Single();
        msg.ReplyTo.Mailboxes.Should().BeEmpty();
    }

    [Fact]
    public async Task SendTransactional_redirects_to_test_recipient_when_set()
    {
        var (sut, factory) = MakeSut(DefaultSettings(testRecipient: "stage@example.org"));
        await sut.SendTransactionalAsync(TransactionalSample());
        factory.SentMessages.Single().To.Mailboxes.Single().Address.Should().Be("stage@example.org");
    }

    [Fact]
    public async Task SendBroadcast_sends_one_message_per_recipient()
    {
        var (sut, factory) = MakeSut(DefaultSettings());
        var recipients = Enumerable.Range(0, 3)
            .Select(i => new EmailRecipient($"u{i}@example.org", $"U{i}", null))
            .ToList();
        var b = new BroadcastEmailMessage("S", "<p>B</p>", "B", recipients, Guid.NewGuid(), EmailCategory.Broadcast);

        var result = await sut.SendBroadcastAsync(b);

        result.Recipients.Should().HaveCount(3);
        result.Recipients.Should().AllSatisfy(r => r.Success.Should().BeTrue());
        factory.SentMessages.Should().HaveCount(3);
    }

    [Fact]
    public async Task SendBroadcast_emits_additional_headers_verbatim()
    {
        var (sut, factory) = MakeSut(DefaultSettings());
        var headers = new Dictionary<string, string>
        {
            ["List-Unsubscribe"] = "<mailto:unsubscribe@example.org>",
            ["List-Unsubscribe-Post"] = "List-Unsubscribe=One-Click",
        };
        var b = new BroadcastEmailMessage(
            "S", "<p>B</p>", "B",
            new[] { new EmailRecipient("a@x.org", "A", null) },
            Guid.NewGuid(),
            EmailCategory.Broadcast,
            AdditionalHeaders: headers);

        await sut.SendBroadcastAsync(b);

        var msg = factory.SentMessages.Single();
        msg.Headers.Contains("List-Unsubscribe").Should().BeTrue();
        msg.Headers["List-Unsubscribe"].Should().Be("<mailto:unsubscribe@example.org>");
        msg.Headers["List-Unsubscribe-Post"].Should().Be("List-Unsubscribe=One-Click");
    }

    [Fact]
    public async Task SendBroadcast_substitutes_merge_fields_per_recipient()
    {
        var (sut, factory) = MakeSut(DefaultSettings());
        var recipients = new[]
        {
            new EmailRecipient("a@x.org", "A", null,
                new Dictionary<string, string> { ["firstName"] = "Alice" }),
            new EmailRecipient("b@x.org", "B", null,
                new Dictionary<string, string> { ["firstName"] = "Bob" }),
        };
        var b = new BroadcastEmailMessage(
            "S", "<p>Hi {{firstName}}</p>", "Hi {{firstName}}",
            recipients, Guid.NewGuid(), EmailCategory.Broadcast);

        await sut.SendBroadcastAsync(b);

        factory.SentMessages.Should().HaveCount(2);
        factory.SentMessages[0].HtmlBody.Should().Contain("Hi Alice");
        factory.SentMessages[1].HtmlBody.Should().Contain("Hi Bob");
    }

    [Fact]
    public async Task SendBroadcast_marks_recipient_failed_when_send_throws_but_continues_loop()
    {
        var factory = new CapturingFactory();
        var attempts = 0;
        factory.Client
            .Setup(c => c.SendAsync(It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>()))
            .Returns<MimeMessage, CancellationToken>((m, _) =>
            {
                attempts++;
                return attempts == 1
                    ? Task.FromException<string>(new InvalidOperationException("boom"))
                    : Task.FromResult("ok");
            });
        var settings = new Mock<ISiteSettingsRepository>();
        settings.Setup(s => s.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(DefaultSettings());
        var sut = new SmtpEmailService(factory, settings.Object, NullLogger<SmtpEmailService>.Instance);

        var b = new BroadcastEmailMessage(
            "S", "<p>B</p>", "B",
            new[] { new EmailRecipient("a@x.org", "A", null), new EmailRecipient("b@x.org", "B", null) },
            Guid.NewGuid(),
            EmailCategory.Broadcast);

        var result = await sut.SendBroadcastAsync(b);

        result.Recipients[0].Success.Should().BeFalse();
        result.Recipients[0].ErrorMessage.Should().Be("boom");
        result.Recipients[1].Success.Should().BeTrue();
    }

    [Fact]
    public async Task SendBroadcast_marks_remaining_recipients_failed_when_connect_throws()
    {
        var factory = new CapturingFactory();
        factory.Client
            .Setup(c => c.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<SecureSocketOptions>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("network down"));
        var settings = new Mock<ISiteSettingsRepository>();
        settings.Setup(s => s.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(DefaultSettings());
        var sut = new SmtpEmailService(factory, settings.Object, NullLogger<SmtpEmailService>.Instance);

        var b = new BroadcastEmailMessage(
            "S", "<p>B</p>", "B",
            new[] { new EmailRecipient("a@x.org", "A", null), new EmailRecipient("b@x.org", "B", null) },
            Guid.NewGuid(),
            EmailCategory.Broadcast);

        var result = await sut.SendBroadcastAsync(b);

        result.Recipients.Should().HaveCount(2);
        result.Recipients.Should().AllSatisfy(r => r.Success.Should().BeFalse());
        result.Recipients.Should().AllSatisfy(r => r.ErrorMessage.Should().Be("network down"));
    }
}
