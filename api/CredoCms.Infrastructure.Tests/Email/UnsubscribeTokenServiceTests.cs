using CredoCms.Application.SiteSettingsManagement;
using CredoCms.Domain.Email;
using CredoCms.Domain.Settings;
using CredoCms.Infrastructure.Email;
using Moq;

namespace CredoCms.Infrastructure.Tests.Email;

public sealed class UnsubscribeTokenServiceTests
{
    private static (UnsubscribeTokenService Sut, Mock<ISiteSettingsRepository> Settings, SiteSettings State) MakeSut()
    {
        var key = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        var state = new SiteSettings { UnsubscribeSigningKey = key };
        var repo = new Mock<ISiteSettingsRepository>();
        repo.Setup(r => r.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(state);
        repo.Setup(r => r.UpdateAsync(It.IsAny<SiteSettings>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return (new UnsubscribeTokenService(repo.Object), repo, state);
    }

    [Fact]
    public async Task Round_trip_token_validates()
    {
        var (sut, _, _) = MakeSut();
        var userId = Guid.NewGuid();
        var token = await sut.GenerateAsync(userId, EmailCategory.Broadcast);
        var validated = await sut.ValidateAsync(token);

        validated.IsValid.Should().BeTrue();
        validated.UserId.Should().Be(userId);
        validated.Category.Should().Be(EmailCategory.Broadcast);
    }

    [Fact]
    public async Task Tampered_token_fails_signature_check()
    {
        var (sut, _, _) = MakeSut();
        var token = await sut.GenerateAsync(Guid.NewGuid(), EmailCategory.News);
        // Flip a character — should change the underlying payload OR signature.
        var tampered = token[..^3] + (token[^3] == 'a' ? "b" : "a") + token[^2..];
        var validated = await sut.ValidateAsync(tampered);
        validated.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Blank_token_fails()
    {
        var (sut, _, _) = MakeSut();
        var validated = await sut.ValidateAsync(string.Empty);
        validated.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Malformed_token_fails()
    {
        var (sut, _, _) = MakeSut();
        var validated = await sut.ValidateAsync("not-base64-at-all-!!!");
        validated.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Auto_generates_key_when_missing_and_persists()
    {
        var state = new SiteSettings { UnsubscribeSigningKey = null };
        var repo = new Mock<ISiteSettingsRepository>();
        repo.Setup(r => r.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(state);
        repo.Setup(r => r.UpdateAsync(It.IsAny<SiteSettings>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var sut = new UnsubscribeTokenService(repo.Object);

        var token = await sut.GenerateAsync(Guid.NewGuid(), EmailCategory.Broadcast);

        token.Should().NotBeNullOrWhiteSpace();
        state.UnsubscribeSigningKey.Should().NotBeNullOrWhiteSpace();
        repo.Verify(r => r.UpdateAsync(It.IsAny<SiteSettings>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
