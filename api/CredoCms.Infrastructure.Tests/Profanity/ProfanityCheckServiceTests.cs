using CredoCms.Application.SiteSettingsManagement;
using CredoCms.Domain.Settings;
using CredoCms.Infrastructure.Profanity;
using Moq;

namespace CredoCms.Infrastructure.Tests.Profanity;

public sealed class ProfanityCheckServiceTests
{
    private static (ProfanityCheckService Sut, SiteSettings Settings) MakeSut(
        string? wordlist = null,
        string? allowlist = null)
    {
        var settings = new SiteSettings
        {
            ProfanityWordlist = wordlist,
            ProfanityAllowlist = allowlist,
        };
        var repo = new Mock<ISiteSettingsRepository>();
        repo.Setup(r => r.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(settings);
        return (new ProfanityCheckService(repo.Object), settings);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Returns_false_for_blank_input(string? input)
    {
        var (sut, _) = MakeSut();
        (await sut.ContainsProfanityAsync(input)).Should().BeFalse();
    }

    [Fact]
    public async Task Returns_false_for_clean_text()
    {
        var (sut, _) = MakeSut();
        (await sut.ContainsProfanityAsync("Please pray for my grandmother who is in the hospital."))
            .Should().BeFalse();
    }

    [Fact]
    public async Task Returns_true_for_baseline_profanity()
    {
        // Word from the package's built-in canonical list.
        var (sut, _) = MakeSut();
        (await sut.ContainsProfanityAsync("this is fucking ridiculous")).Should().BeTrue();
    }

    [Fact]
    public async Task Custom_wordlist_from_settings_is_picked_up()
    {
        var (sut, _) = MakeSut(wordlist: "florple\nbazquux");
        (await sut.ContainsProfanityAsync("you absolute florple")).Should().BeTrue();
    }

    [Fact]
    public async Task Allowlist_suppresses_a_baseline_word()
    {
        // Without allowlist the package would flag "shit"; allowlist clears it.
        var clean = MakeSut().Sut;
        (await clean.ContainsProfanityAsync("oh shit")).Should().BeTrue();

        var allowed = MakeSut(allowlist: "shit").Sut;
        (await allowed.ContainsProfanityAsync("oh shit")).Should().BeFalse();
    }

    [Fact]
    public async Task Custom_word_can_be_silenced_via_allowlist()
    {
        var sut = MakeSut(wordlist: "florple", allowlist: "florple").Sut;
        (await sut.ContainsProfanityAsync("you florple")).Should().BeFalse();
    }

    [Theory]
    [InlineData("hello world")]      // contains "hell"
    [InlineData("scunthorpe")]       // contains "cunt"
    [InlineData("classic literature")] // contains "ass"
    public async Task Scunthorpe_problem_words_do_not_trigger(string clean)
    {
        // Regression guard: the package's ContainsProfanity uses naive
        // substring matching and would flag these. We call
        // DetectAllProfanities with partial-match elimination instead.
        var (sut, _) = MakeSut();
        (await sut.ContainsProfanityAsync(clean)).Should().BeFalse();
    }
}
