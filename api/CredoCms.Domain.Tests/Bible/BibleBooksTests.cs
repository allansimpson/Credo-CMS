using CredoCms.Domain.Bible;

namespace CredoCms.Domain.Tests.Bible;

public sealed class BibleBooksTests
{
    [Fact]
    public void Has_66_books_total()
    {
        BibleBooks.All.Count.Should().Be(66);
    }

    [Fact]
    public void Has_39_old_testament_and_27_new_testament()
    {
        BibleBooks.All.Count(b => b.Testament == Testament.OldTestament).Should().Be(39);
        BibleBooks.All.Count(b => b.Testament == Testament.NewTestament).Should().Be(27);
    }

    [Fact]
    public void Slugs_are_unique()
    {
        BibleBooks.All.Select(b => b.Slug).Distinct().Count().Should().Be(66);
    }

    [Theory]
    [InlineData(BibleBook.Genesis, 50)]
    [InlineData(BibleBook.Psalms, 150)]
    [InlineData(BibleBook.Matthew, 28)]
    [InlineData(BibleBook.Revelation, 22)]
    [InlineData(BibleBook.Obadiah, 1)]
    [InlineData(BibleBook.Philemon, 1)]
    public void Chapter_counts_match_canon(BibleBook book, int expected)
    {
        BibleBooks.Get(book).ChapterCount.Should().Be(expected);
    }

    [Fact]
    public void FindBySlug_is_case_insensitive_and_returns_null_for_unknown()
    {
        BibleBooks.FindBySlug("matthew")!.Book.Should().Be(BibleBook.Matthew);
        BibleBooks.FindBySlug("MATTHEW")!.Book.Should().Be(BibleBook.Matthew);
        BibleBooks.FindBySlug("song-of-solomon")!.Book.Should().Be(BibleBook.SongOfSolomon);
        BibleBooks.FindBySlug("not-a-book").Should().BeNull();
    }
}
