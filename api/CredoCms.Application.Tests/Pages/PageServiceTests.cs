using CredoCms.Application.Common;
using CredoCms.Application.Pages;
using CredoCms.Domain.Pages;
using Moq;

namespace CredoCms.Application.Tests.Pages;

public sealed class PageServiceTests
{
    private static (PageService sut, Mock<IPageRepository> repo, Mock<IAuditLogger> audit) MakeSut()
    {
        var repo = new Mock<IPageRepository>();
        var audit = new Mock<IAuditLogger>();
        var sut = new PageService(
            repo.Object, audit.Object,
            new CreatePageRequestValidator(),
            new UpdatePageRequestValidator());
        return (sut, repo, audit);
    }

    private static CreatePageRequest ValidCreate(string slug = "about") => new(
        slug, "About", """{"type":"doc","content":[]}""",
        null, null, null, null, null, true, false);

    [Fact]
    public async Task CreateAsync_rejects_invalid_slug()
    {
        var (sut, _, _) = MakeSut();
        var result = await sut.CreateAsync(ValidCreate(slug: "Has Spaces"));
        result.Succeeded.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CreateAsync_rejects_duplicate_slug()
    {
        var (sut, repo, _) = MakeSut();
        repo.Setup(x => x.SlugExistsAsync("about", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var result = await sut.CreateAsync(ValidCreate());
        result.Succeeded.Should().BeFalse();
        result.Errors[0].Should().Contain("already exists");
    }

    [Fact]
    public async Task CreateAsync_persists_and_audits()
    {
        var (sut, repo, audit) = MakeSut();
        repo.Setup(x => x.SlugExistsAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await sut.CreateAsync(ValidCreate());

        result.Succeeded.Should().BeTrue();
        result.Page.Should().NotBeNull();
        result.Page!.Slug.Should().Be("about");
        result.Page.IsPublished.Should().BeTrue();
        result.Page.PublishedAt.Should().NotBeNull();

        repo.Verify(x => x.AddAsync(It.IsAny<Page>(), It.IsAny<CancellationToken>()), Times.Once);
        audit.Verify(x => x.WriteAsync("Page.Created", "Page", It.IsAny<string?>(),
            It.IsAny<object?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_blocks_slug_change_on_system_pages()
    {
        var (sut, repo, _) = MakeSut();
        var existing = new Page
        {
            Id = Guid.NewGuid(),
            Slug = "privacy",
            Title = "Privacy",
            BodyJson = "{}",
            IsSystemPage = true,
            IsPublished = true,
            CreatedAt = DateTimeOffset.UtcNow,
            ModifiedAt = DateTimeOffset.UtcNow,
        };
        repo.Setup(x => x.GetByIdAsync(existing.Id, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var result = await sut.UpdateAsync(existing.Id, new UpdatePageRequest(
            "renamed", existing.Title, existing.BodyJson, null, null, null, null, null,
            true, false));

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("System page", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task SoftDeleteAsync_marks_deleted_and_unpublishes()
    {
        var (sut, repo, _) = MakeSut();
        var existing = new Page
        {
            Id = Guid.NewGuid(),
            Slug = "about",
            Title = "About",
            BodyJson = "{}",
            IsPublished = true,
            CreatedAt = DateTimeOffset.UtcNow,
            ModifiedAt = DateTimeOffset.UtcNow,
        };
        repo.Setup(x => x.GetByIdAsync(existing.Id, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var result = await sut.SoftDeleteAsync(existing.Id);

        result.Succeeded.Should().BeTrue();
        existing.IsDeleted.Should().BeTrue();
        existing.IsPublished.Should().BeFalse();
        existing.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task HardDeleteAsync_blocks_system_pages()
    {
        var (sut, repo, _) = MakeSut();
        var existing = new Page
        {
            Id = Guid.NewGuid(),
            Slug = "privacy",
            Title = "Privacy",
            BodyJson = "{}",
            IsSystemPage = true,
            IsDeleted = true,
            CreatedAt = DateTimeOffset.UtcNow,
            ModifiedAt = DateTimeOffset.UtcNow,
        };
        repo.Setup(x => x.GetByIdAsync(existing.Id, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var result = await sut.HardDeleteAsync(existing.Id);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("System pages", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task HardDeleteAsync_requires_soft_delete_first()
    {
        var (sut, repo, _) = MakeSut();
        var existing = new Page
        {
            Id = Guid.NewGuid(),
            Slug = "about",
            Title = "About",
            BodyJson = "{}",
            IsDeleted = false,
            CreatedAt = DateTimeOffset.UtcNow,
            ModifiedAt = DateTimeOffset.UtcNow,
        };
        repo.Setup(x => x.GetByIdAsync(existing.Id, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var result = await sut.HardDeleteAsync(existing.Id);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Soft-delete", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetPublicBySlugAsync_returns_null_for_unpublished()
    {
        var (sut, repo, _) = MakeSut();
        repo.Setup(x => x.GetBySlugAsync("draft", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Page { Slug = "draft", Title = "Draft", BodyJson = "{}", IsPublished = false });

        var result = await sut.GetPublicBySlugAsync("draft", includeMembersOnly: true);
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetPublicBySlugAsync_returns_null_for_members_only_when_not_authenticated()
    {
        var (sut, repo, _) = MakeSut();
        repo.Setup(x => x.GetBySlugAsync("members-only", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Page { Slug = "members-only", Title = "Members", BodyJson = "{}", IsPublished = true, IsMembersOnly = true });

        var result = await sut.GetPublicBySlugAsync("members-only", includeMembersOnly: false);
        result.Should().BeNull();
    }

    [Fact]
    public void AutoExcerpt_extracts_text_from_prosemirror_doc()
    {
        const string doc =
            "{\"type\":\"doc\",\"content\":[" +
            "{\"type\":\"paragraph\",\"content\":[{\"type\":\"text\",\"text\":\"Hello \"},{\"type\":\"text\",\"text\":\"world.\"}]}," +
            "{\"type\":\"paragraph\",\"content\":[{\"type\":\"text\",\"text\":\"Second.\"}]}" +
            "]}";
        var excerpt = PageService.AutoExcerpt(doc, 100);
        excerpt.Should().Contain("Hello world.").And.Contain("Second.");
    }

    [Fact]
    public void AutoExcerpt_truncates_at_max_length()
    {
        var longText = new string('a', 500);
        var doc =
            "{\"type\":\"doc\",\"content\":[{\"type\":\"paragraph\",\"content\":[{\"type\":\"text\",\"text\":\""
            + longText + "\"}]}]}";
        var excerpt = PageService.AutoExcerpt(doc, 50);
        excerpt.Length.Should().BeLessThanOrEqualTo(60);
        excerpt.Should().EndWith("…");
    }

    [Fact]
    public void AutoExcerpt_returns_empty_for_invalid_json()
    {
        PageService.AutoExcerpt("not-json").Should().BeEmpty();
    }
}
