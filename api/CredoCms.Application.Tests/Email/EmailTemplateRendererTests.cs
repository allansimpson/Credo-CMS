using CredoCms.Application.Email;
using CredoCms.Application.SiteSettingsManagement;
using CredoCms.Domain.Email;
using CredoCms.Domain.Settings;
using Moq;

namespace CredoCms.Application.Tests.Email;

public sealed class EmailTemplateRendererTests
{
    private static (EmailTemplateRenderer Sut, Mock<IEmailTemplateRepository> Repo, Mock<ISiteSettingsRepository> Settings) MakeSut()
    {
        var repo = new Mock<IEmailTemplateRepository>();
        var settings = new Mock<ISiteSettingsRepository>();
        settings.Setup(s => s.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SiteSettings { ChurchName = "Hope Community" });
        return (new EmailTemplateRenderer(repo.Object, settings.Object), repo, settings);
    }

    private static EmailTemplate Template(string subject, string html, string? text = null) => new()
    {
        Id = Guid.NewGuid(),
        TemplateKey = "TestKey",
        Subject = subject,
        HtmlBody = html,
        PlainTextBody = text,
    };

    [Fact]
    public async Task Substitutes_simple_token()
    {
        var (sut, repo, _) = MakeSut();
        repo.Setup(r => r.GetByKeyAsync("TestKey", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Template("Hi {{firstName}}", "<p>Hi {{firstName}}</p>"));

        var rendered = await sut.RenderAsync("TestKey",
            new Dictionary<string, string> { ["firstName"] = "Alice" });

        rendered.Subject.Should().Be("Hi Alice");
        rendered.HtmlBody.Should().Contain("Hi Alice");
    }

    [Fact]
    public async Task Auto_injects_church_name_and_year()
    {
        var (sut, repo, _) = MakeSut();
        repo.Setup(r => r.GetByKeyAsync("TestKey", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Template("Welcome to {{churchName}}", "<p>{{currentYear}}</p>"));

        var rendered = await sut.RenderAsync("TestKey", new Dictionary<string, string>());

        rendered.Subject.Should().Be("Welcome to Hope Community");
        rendered.HtmlBody.Should().Contain(DateTime.UtcNow.Year.ToString());
    }

    [Fact]
    public async Task Throws_when_referenced_variable_missing()
    {
        var (sut, repo, _) = MakeSut();
        repo.Setup(r => r.GetByKeyAsync("TestKey", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Template("Hi {{firstName}}", "<p>Hi {{firstName}}</p>"));

        await FluentActions.Invoking(() => sut.RenderAsync("TestKey", new Dictionary<string, string>()))
            .Should().ThrowAsync<TemplateRenderException>();
    }

    [Fact]
    public async Task Throws_when_template_not_found()
    {
        var (sut, repo, _) = MakeSut();
        repo.Setup(r => r.GetByKeyAsync("Missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((EmailTemplate?)null);

        await FluentActions.Invoking(() => sut.RenderAsync("Missing", new Dictionary<string, string>()))
            .Should().ThrowAsync<TemplateRenderException>();
    }

    [Fact]
    public async Task Plain_text_derived_from_html_when_not_supplied()
    {
        var (sut, repo, _) = MakeSut();
        repo.Setup(r => r.GetByKeyAsync("TestKey", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Template("S", "<p>Hi <strong>Alice</strong></p><p>Welcome.</p>", text: null));

        var rendered = await sut.RenderAsync("TestKey", new Dictionary<string, string>());

        rendered.PlainTextBody.Should().Contain("Hi Alice");
        rendered.PlainTextBody.Should().Contain("Welcome.");
        rendered.PlainTextBody.Should().NotContain("<");
    }

    [Fact]
    public async Task Caller_can_override_common_field()
    {
        var (sut, repo, _) = MakeSut();
        repo.Setup(r => r.GetByKeyAsync("TestKey", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Template("From {{churchName}}", "<p>x</p>"));

        var rendered = await sut.RenderAsync("TestKey",
            new Dictionary<string, string> { ["churchName"] = "Other Church" });

        rendered.Subject.Should().Be("From Other Church");
    }

    [Fact]
    public void Substitute_handles_unterminated_tokens()
    {
        // Defensive: an unterminated {{ should not throw, just be left in place.
        var ctx = new Dictionary<string, string> { ["x"] = "y" };
        var result = EmailTemplateRenderer.Substitute("k", "before {{x}} mid {{ unterminated", ctx);
        result.Should().Be("before y mid {{ unterminated");
    }
}
