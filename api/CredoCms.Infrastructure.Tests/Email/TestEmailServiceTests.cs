using CredoCms.Application.Email;
using CredoCms.Domain.Email;
using CredoCms.Infrastructure.Email;
using MailKit.Security;
using Microsoft.Extensions.Logging.Abstractions;
using MimeKit;
using Moq;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace CredoCms.Infrastructure.Tests.Email;

public sealed class TestEmailServiceTests
{
    private static (TestEmailService Sut, Mock<ISendGridClient> SgClient, Mock<IMailKitSmtpClient> SmtpClient) MakeSut()
    {
        var sgClient = new Mock<ISendGridClient>();
        var sgFactory = new Mock<ISendGridClientFactory>();
        sgFactory.Setup(f => f.Create(It.IsAny<string>())).Returns(sgClient.Object);

        var smtpClient = new Mock<IMailKitSmtpClient>();
        smtpClient.Setup(c => c.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<SecureSocketOptions>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        smtpClient.Setup(c => c.AuthenticateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        smtpClient.Setup(c => c.SendAsync(It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>())).ReturnsAsync("smtp-id");
        smtpClient.Setup(c => c.DisconnectAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var smtpFactory = new Mock<IMailKitSmtpClientFactory>();
        smtpFactory.Setup(f => f.Create()).Returns(smtpClient.Object);

        var sut = new TestEmailService(sgFactory.Object, smtpFactory.Object, NullLoggerFactory.Instance);
        return (sut, sgClient, smtpClient);
    }

    private static TestEmailConfig SgConfig() => new(
        Provider: EmailProvider.SendGrid,
        EmailFromAddress: "noreply@example.org",
        EmailFromName: "Church",
        EmailReplyToAddress: null,
        SendGridApiKey: "SG.test",
        SmtpHost: null,
        SmtpPort: 587,
        SmtpUsername: null,
        SmtpPassword: null,
        SmtpUseSsl: true,
        TestEmailRecipient: null);

    private static TestEmailConfig SmtpConfig() => new(
        Provider: EmailProvider.Smtp,
        EmailFromAddress: "noreply@example.org",
        EmailFromName: "Church",
        EmailReplyToAddress: null,
        SendGridApiKey: null,
        SmtpHost: "smtp.example.org",
        SmtpPort: 587,
        SmtpUsername: "u",
        SmtpPassword: "p",
        SmtpUseSsl: true,
        TestEmailRecipient: null);

    private static Response Accepted()
    {
        var headers = new System.Net.Http.HttpResponseMessage().Headers;
        return new Response(System.Net.HttpStatusCode.Accepted, new System.Net.Http.StringContent(""), headers);
    }

    [Fact]
    public async Task None_provider_returns_success_without_dispatch()
    {
        var (sut, sg, smtp) = MakeSut();
        var config = SgConfig() with { Provider = EmailProvider.None };

        var result = await sut.SendAsync(config, "admin@example.org", "Admin", CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Note.Should().Contain("None");
        sg.Verify(c => c.SendEmailAsync(It.IsAny<SendGridMessage>(), It.IsAny<CancellationToken>()), Times.Never);
        smtp.Verify(c => c.SendAsync(It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SendGrid_path_dispatches_one_message()
    {
        var (sut, sg, _) = MakeSut();
        sg.Setup(c => c.SendEmailAsync(It.IsAny<SendGridMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Accepted());

        var result = await sut.SendAsync(SgConfig(), "admin@example.org", "Admin", CancellationToken.None);

        result.Success.Should().BeTrue();
        sg.Verify(c => c.SendEmailAsync(It.IsAny<SendGridMessage>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendGrid_path_returns_failure_when_provider_rejects()
    {
        var (sut, sg, _) = MakeSut();
        sg.Setup(c => c.SendEmailAsync(It.IsAny<SendGridMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Response(System.Net.HttpStatusCode.Forbidden, new System.Net.Http.StringContent(""), new System.Net.Http.HttpResponseMessage().Headers));

        var result = await sut.SendAsync(SgConfig(), "admin@example.org", "Admin", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("403");
    }

    [Fact]
    public async Task SMTP_path_dispatches_one_message()
    {
        var (sut, _, smtp) = MakeSut();

        var result = await sut.SendAsync(SmtpConfig(), "admin@example.org", "Admin", CancellationToken.None);

        result.Success.Should().BeTrue();
        smtp.Verify(c => c.SendAsync(It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SMTP_path_returns_failure_when_connect_throws()
    {
        var sgClient = new Mock<ISendGridClient>();
        var sgFactory = new Mock<ISendGridClientFactory>();
        sgFactory.Setup(f => f.Create(It.IsAny<string>())).Returns(sgClient.Object);

        var smtpClient = new Mock<IMailKitSmtpClient>();
        smtpClient.Setup(c => c.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<SecureSocketOptions>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("connection refused"));
        smtpClient.Setup(c => c.DisconnectAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var smtpFactory = new Mock<IMailKitSmtpClientFactory>();
        smtpFactory.Setup(f => f.Create()).Returns(smtpClient.Object);
        var sut = new TestEmailService(sgFactory.Object, smtpFactory.Object, NullLoggerFactory.Instance);

        var result = await sut.SendAsync(SmtpConfig(), "admin@example.org", "Admin", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("connection refused");
    }
}
