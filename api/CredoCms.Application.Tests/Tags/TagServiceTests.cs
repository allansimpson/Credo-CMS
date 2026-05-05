using CredoCms.Application.Tags;
using CredoCms.Domain.Tags;
using Moq;

namespace CredoCms.Application.Tests.Tags;

public sealed class TagServiceTests
{
    [Fact]
    public void Slugify_lowercases_and_dashes_non_alphanumerics()
    {
        TagService.Slugify("Plan Your Visit").Should().Be("plan-your-visit");
        TagService.Slugify("1 John (Letter)").Should().Be("1-john-letter");
        TagService.Slugify("  Easter  ").Should().Be("easter");
    }

    [Fact]
    public void Slugify_falls_back_to_tag_when_no_alphanumerics()
    {
        TagService.Slugify("!!!").Should().Be("tag");
    }

    [Fact]
    public void TitleCase_keeps_short_acronyms()
    {
        TagService.ToTitleCase("ot promises").Should().Be("Ot Promises");
        TagService.ToTitleCase("OT promises").Should().Be("OT Promises");
        TagService.ToTitleCase("nyc").Should().Be("Nyc");
        // Words longer than 3 chars title-case even if all-caps in input.
        TagService.ToTitleCase("FAITH and HOPE").Should().Be("Faith And Hope");
    }

    [Fact]
    public async Task NormalizeAndUpsertAsync_returns_existing_tag_for_case_match()
    {
        var existing = new Tag { Id = Guid.NewGuid(), Name = "Easter", Slug = "easter" };
        var repo = new Mock<ITagRepository>();
        repo.Setup(r => r.GetByNameInsensitiveAsync("Easter", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var sut = new TagService(repo.Object);
        var result = await sut.NormalizeAndUpsertAsync("easter");

        result.Should().BeSameAs(existing);
        repo.Verify(r => r.AddAsync(It.IsAny<Tag>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task NormalizeAndUpsertAsync_creates_new_tag_when_missing()
    {
        var repo = new Mock<ITagRepository>();
        repo.Setup(r => r.GetByNameInsensitiveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tag?)null);

        var sut = new TagService(repo.Object);
        var result = await sut.NormalizeAndUpsertAsync("  matthew ");

        result.Name.Should().Be("Matthew");
        result.Slug.Should().Be("matthew");
        result.UsageCount.Should().Be(0);
        repo.Verify(r => r.AddAsync(It.Is<Tag>(t => t.Name == "Matthew"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task NormalizeAndUpsertAsync_throws_on_blank_input()
    {
        var sut = new TagService(Mock.Of<ITagRepository>());
        await Assert.ThrowsAsync<ArgumentException>(() => sut.NormalizeAndUpsertAsync("   "));
    }
}
