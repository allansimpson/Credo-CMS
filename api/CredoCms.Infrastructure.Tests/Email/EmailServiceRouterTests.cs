using CredoCms.Application.Common;
using CredoCms.Application.SiteSettingsManagement;
using CredoCms.Domain.Email;
using CredoCms.Domain.Settings;
using CredoCms.Infrastructure.Email;
using MailKit.Security;
using Microsoft.Extensions.Logging.Abstractions;
using MimeKit;
using Moq;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace CredoCms.Infrastructure.Tests.Email;

public sealed class EmailServiceRouterTests
{
    private static EmailMessage TransactionalSample() => new(
        ToAddress: "user@example.org",
        ToName: "User",
        Subject: "Subject",
        HtmlBody: "<p>Hello</p>",
        PlainTextBody: "Hello",
        UserId: null,
        Category: EmailCategory.Transactional);

    private sealed class StubFactories
    {
        public Mock<ISendGridClient> SendGridClient { get; } = new();
        public Mock<IMailKitSmtpClient> SmtpClient { get; } = new();
        public Mock<ISendGridClientFactory> SendGridFactory { get; }
        public Mock<IMailKitSmtpClientFactory> SmtpFactory { get; }

        public StubFactories()
        {
            // SendGrid: 202 Accepted with X-Message-Id.
            var headers = new System.Net.Http.HttpResponseMessage().Headers;
            headers.Add("X-Message-Id", "stub");
            SendGridClient
                .Setup(c => c.SendEmailAsync(It.IsAny<SendGridMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Response(System.Net.HttpStatusCode.Accepted, new System.Net.Http.StringContent(""), headers));
            SendGridFactory = new Mock<ISendGridClientFactory>();
            SendGridFactory.Setup(f => f.Create(It.IsAny<string>())).Returns(SendGridClient.Object);

            // SMTP: connect/auth/send/disconnect all succeed.
            SmtpClient.Setup(c => c.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<SecureSocketOptions>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            SmtpClient.Setup(c => c.AuthenticateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            SmtpClient.Setup(c => c.SendAsync(It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>())).ReturnsAsync("smtp-id");
            SmtpClient.Setup(c => c.DisconnectAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            SmtpFactory = new Mock<IMailKitSmtpClientFactory>();
            SmtpFactory.Setup(f => f.Create()).Returns(SmtpClient.Object);
        }
    }

    private static (EmailServiceRouter Router, StubFactories Stubs) MakeSut(SiteSettings settings)
    {
        var stubs = new StubFactories();
        var settingsMock = new Mock<ISiteSettingsRepository>();
        settingsMock.Setup(s => s.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(settings);

        var logging = new LoggingEmailService(NullLogger<LoggingEmailService>.Instance, settingsMock.Object);
        var sendGrid = new SendGridEmailService(stubs.SendGridFactory.Object, settingsMock.Object, NullLogger<SendGridEmailService>.Instance);
        var smtp = new SmtpEmailService(stubs.SmtpFactory.Object, settingsMock.Object, NullLogger<SmtpEmailService>.Instance);
        var router = new EmailServiceRouter(logging, sendGrid, smtp, settingsMock.Object, NullLogger<EmailServiceRouter>.Instance);
        return (router, stubs);
    }

    [Fact]
    public async Task Routes_to_SendGrid_when_configured()
    {
        var (router, stubs) = MakeSut(new SiteSettings
        {
            EmailProvider = EmailProvider.SendGrid,
            SendGridApiKey = "SG.test",
            EmailFromAddress = "noreply@example.org",
            EmailFromName = "Church",
            EmailEnabled = true,
        });

        await router.SendTransactionalAsync(TransactionalSample());

        stubs.SendGridClient.Verify(c => c.SendEmailAsync(It.IsAny<SendGridMessage>(), It.IsAny<CancellationToken>()), Times.Once);
        stubs.SmtpClient.Verify(c => c.SendAsync(It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Routes_to_SMTP_when_configured()
    {
        var (router, stubs) = MakeSut(new SiteSettings
        {
            EmailProvider = EmailProvider.Smtp,
            SmtpHost = "smtp.example.org",
            SmtpPort = 587,
            SmtpUseSsl = true,
            EmailFromAddress = "noreply@example.org",
            EmailFromName = "Church",
            EmailEnabled = true,
        });

        await router.SendTransactionalAsync(TransactionalSample());

        stubs.SmtpClient.Verify(c => c.SendAsync(It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>()), Times.Once);
        stubs.SendGridClient.Verify(c => c.SendEmailAsync(It.IsAny<SendGridMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Falls_back_to_logging_when_SendGrid_chosen_but_key_missing()
    {
        var (router, stubs) = MakeSut(new SiteSettings
        {
            EmailProvider = EmailProvider.SendGrid,
            SendGridApiKey = null,
            EmailFromAddress = "noreply@example.org",
            EmailFromName = "Church",
            EmailEnabled = true,
        });

        await router.SendTransactionalAsync(TransactionalSample());

        // No SendGrid call; LoggingEmailService is no-op (just logs).
        stubs.SendGridClient.Verify(c => c.SendEmailAsync(It.IsAny<SendGridMessage>(), It.IsAny<CancellationToken>()), Times.Never);
        stubs.SmtpClient.Verify(c => c.SendAsync(It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Falls_back_to_logging_when_SMTP_chosen_but_host_missing()
    {
        var (router, stubs) = MakeSut(new SiteSettings
        {
            EmailProvider = EmailProvider.Smtp,
            SmtpHost = "",
            EmailFromAddress = "noreply@example.org",
            EmailFromName = "Church",
            EmailEnabled = true,
        });

        await router.SendTransactionalAsync(TransactionalSample());

        stubs.SendGridClient.Verify(c => c.SendEmailAsync(It.IsAny<SendGridMessage>(), It.IsAny<CancellationToken>()), Times.Never);
        stubs.SmtpClient.Verify(c => c.SendAsync(It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Routes_to_logging_when_provider_is_none()
    {
        var (router, stubs) = MakeSut(new SiteSettings
        {
            EmailProvider = EmailProvider.None,
            EmailFromAddress = "noreply@example.org",
            EmailFromName = "Church",
            EmailEnabled = true,
        });

        await router.SendTransactionalAsync(TransactionalSample());

        stubs.SendGridClient.Verify(c => c.SendEmailAsync(It.IsAny<SendGridMessage>(), It.IsAny<CancellationToken>()), Times.Never);
        stubs.SmtpClient.Verify(c => c.SendAsync(It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task IsConfigured_reflects_target_provider()
    {
        var (sg, _) = MakeSut(new SiteSettings
        {
            EmailProvider = EmailProvider.SendGrid,
            SendGridApiKey = "SG.test",
            EmailFromAddress = "noreply@example.org",
            EmailFromName = "Church",
            EmailEnabled = true,
        });
        (await sg.IsConfiguredAsync()).Should().BeTrue();

        var (none, _) = MakeSut(new SiteSettings
        {
            EmailProvider = EmailProvider.None,
            EmailFromAddress = "noreply@example.org",
            EmailFromName = "Church",
            EmailEnabled = true,
        });
        // LoggingEmailService is always considered "configured."
        (await none.IsConfiguredAsync()).Should().BeTrue();
    }
}
