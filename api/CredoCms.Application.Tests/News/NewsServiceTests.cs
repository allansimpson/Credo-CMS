using CredoCms.Application.Common;
using CredoCms.Application.News;
using CredoCms.Domain.News;
using Moq;

namespace CredoCms.Application.Tests.News;

public sealed class NewsServiceTests
{
    private static (NewsService sut, Mock<INewsRepository> repo, Mock<IAuditLogger> audit) MakeSut()
    {
        var repo = new Mock<INewsRepository>();
        var audit = new Mock<IAuditLogger>();
        var sut = new NewsService(
            repo.Object, audit.Object,
            new CreateNewsItemRequestValidator(),
            new UpdateNewsItemRequestValidator());
        return (sut, repo, audit);
    }

    private static CreateNewsItemRequest ValidCreate(string slug = "summer-camp-2026") => new(
        slug, "Summer Camp 2026", """{"type":"doc","content":[]}""",
        null, null, null, null, null,
        true, true,
        ExpiresAt: null, CalendarDate: null);

    [Fact]
    public async Task CreateAsync_defaults_to_published_with_published_timestamp()
    {
        var (sut, repo, _) = MakeSut();
        repo.Setup(x => x.SlugExistsAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await sut.CreateAsync(ValidCreate());

        result.Succeeded.Should().BeTrue();
        result.Item.Should().NotBeNull();
        result.Item!.IsMembersOnly.Should().BeTrue();
        result.Item.PublishedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task GetPublicBySlugAsync_returns_null_for_expired_item()
    {
        var (sut, repo, _) = MakeSut();
        repo.Setup(x => x.GetBySlugAsync("expired", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NewsItem
            {
                Slug = "expired", Title = "Old", BodyJson = "{}",
                IsPublished = true, IsMembersOnly = false,
                ExpiresAt = DateTimeOffset.UtcNow.AddHours(-1),
            });

        var result = await sut.GetPublicBySlugAsync("expired", includeMembersOnly: true);
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetPublicBySlugAsync_returns_null_for_members_only_when_anonymous()
    {
        var (sut, repo, _) = MakeSut();
        repo.Setup(x => x.GetBySlugAsync("private", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NewsItem
            {
                Slug = "private", Title = "Private", BodyJson = "{}",
                IsPublished = true, IsMembersOnly = true,
            });

        var result = await sut.GetPublicBySlugAsync("private", includeMembersOnly: false);
        result.Should().BeNull();
    }

    [Fact]
    public async Task SoftDeleteAsync_unpublishes_and_marks_deleted()
    {
        var (sut, repo, _) = MakeSut();
        var item = new NewsItem { Id = Guid.NewGuid(), Slug = "x", Title = "X", BodyJson = "{}", IsPublished = true };
        repo.Setup(x => x.GetByIdAsync(item.Id, false, It.IsAny<CancellationToken>())).ReturnsAsync(item);

        var result = await sut.SoftDeleteAsync(item.Id);

        result.Succeeded.Should().BeTrue();
        item.IsDeleted.Should().BeTrue();
        item.IsPublished.Should().BeFalse();
    }

    [Fact]
    public async Task HardDeleteAsync_requires_soft_delete_first()
    {
        var (sut, repo, _) = MakeSut();
        var item = new NewsItem { Id = Guid.NewGuid(), Slug = "x", Title = "X", BodyJson = "{}", IsDeleted = false };
        repo.Setup(x => x.GetByIdAsync(item.Id, true, It.IsAny<CancellationToken>())).ReturnsAsync(item);

        var result = await sut.HardDeleteAsync(item.Id);
        result.Succeeded.Should().BeFalse();
    }
}
