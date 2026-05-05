using CredoCms.Application.Scripture;
using CredoCms.Domain.Bible;

namespace CredoCms.Application.Tests.Scripture;

public sealed class ScriptureReferenceServiceTests
{
    [Fact]
    public void Validate_accepts_full_chapter()
    {
        var (ok, _) = IScriptureReferenceService.Validate(
            new ScriptureReferenceInput(BibleBook.Romans, 8, null, null, null));
        ok.Should().BeTrue();
    }

    [Fact]
    public void Validate_accepts_cross_chapter_range()
    {
        var (ok, _) = IScriptureReferenceService.Validate(
            new ScriptureReferenceInput(BibleBook.Matthew, 5, 1, 7, 29));
        ok.Should().BeTrue();
    }

    [Fact]
    public void Validate_rejects_chapter_zero()
    {
        var (ok, err) = IScriptureReferenceService.Validate(
            new ScriptureReferenceInput(BibleBook.Genesis, 0, null, null, null));
        ok.Should().BeFalse();
        err.Should().Contain("out of range");
    }

    [Fact]
    public void Validate_rejects_chapter_above_book_max()
    {
        var (ok, err) = IScriptureReferenceService.Validate(
            new ScriptureReferenceInput(BibleBook.Matthew, 50, null, null, null));
        ok.Should().BeFalse();
        err.Should().Contain("Matthew");
    }

    [Fact]
    public void Validate_rejects_inverted_chapter_range()
    {
        var (ok, err) = IScriptureReferenceService.Validate(
            new ScriptureReferenceInput(BibleBook.Romans, 8, null, 5, null));
        ok.Should().BeFalse();
        err.Should().Contain("≥ starting");
    }

    [Fact]
    public void Validate_rejects_inverted_verse_range_within_chapter()
    {
        var (ok, err) = IScriptureReferenceService.Validate(
            new ScriptureReferenceInput(BibleBook.FirstJohn, 2, 17, null, 15));
        ok.Should().BeFalse();
        err.Should().Contain("≥ starting verse");
    }
}
