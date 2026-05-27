using CredoCms.Application.Blog;
using CredoCms.Application.Caching;
using CredoCms.Application.Common;
using CredoCms.Application.Tags;
using CredoCms.Domain.Common;
using CredoCms.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace CredoCms.Application.Tests.Blog;

public sealed class BlogServiceTests
{
    private static (BlogService sut, Mock<IBlogRepository> repo)
        MakeSut(string[] roles)
    {
        var repo = new Mock<IBlogRepository>();
        repo.Setup(x => x.GetTagIdsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Guid>());

        var store = new Mock<IUserStore<ApplicationUser>>();
        var um = new Mock<UserManager<ApplicationUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        um.Setup(x => x.FindByIdAsync(It.IsAny<string>()))
            .ReturnsAsync(new ApplicationUser { FirstName = "Author", LastName = "Person" });

        var tagService = new Mock<ITagService>();
        var tagRepo = new Mock<ITagRepository>();
        tagRepo.Setup(x => x.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CredoCms.Domain.Tags.Tag>());

        var user = new Mock<ICurrentUserService>();
        user.SetupGet(x => x.UserId).Returns(Guid.NewGuid());
        user.SetupGet(x => x.IsAuthenticated).Returns(true);
        user.SetupGet(x => x.Roles).Returns(roles);

        var sut = new BlogService(
            repo.Object, um.Object, tagService.Object, tagRepo.Object,
            user.Object, Mock.Of<IAuditLogger>(), Mock.Of<IOutputCacheInvalidator>(),
            new CreateBlogPostRequestValidator(),
            new UpdateBlogPostRequestValidator());
        return (sut, repo);
    }

    [Fact]
    public async Task CreateAsync_rejects_non_admin_shell()
    {
        var (sut, _) = MakeSut(new[] { SystemConstants.Roles.Member });
        var result = await sut.CreateAsync(new CreateBlogPostRequest(
            "post", "Title", "{}", null, null, null, null, "Devotional",
            null, false, false, false, null, null, null, null));
        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task CreateAsync_succeeds_for_editor_and_persists()
    {
        var (sut, repo) = MakeSut(new[] { SystemConstants.Roles.Editor });
        repo.Setup(x => x.SlugExistsAsync("post", null, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await sut.CreateAsync(new CreateBlogPostRequest(
            "post", "Title", """{"type":"doc","content":[{"type":"paragraph","content":[{"type":"text","text":"hello world"}]}]}""",
            null, null, null, null, "Devotional",
            null, true, false, false, null, null, null, null));

        result.Succeeded.Should().BeTrue();
        repo.Verify(x => x.AddAsync(It.IsAny<CredoCms.Domain.Blog.BlogPost>(),
            It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void ComputeReadingMinutes_returns_one_for_short_body()
    {
        var json = """{"type":"doc","content":[{"type":"paragraph","content":[{"type":"text","text":"hi there"}]}]}""";
        BlogService.ComputeReadingMinutes(json).Should().Be(1);
    }

    [Fact]
    public void ComputeReadingMinutes_scales_with_word_count()
    {
        var words = string.Join(' ', Enumerable.Range(0, 600).Select(_ => "word"));
        var json = $"{{\"type\":\"doc\",\"content\":[{{\"type\":\"paragraph\",\"content\":[{{\"type\":\"text\",\"text\":\"{words}\"}}]}}]}}";
        BlogService.ComputeReadingMinutes(json).Should().Be(3);
    }

    [Fact]
    public async Task GetPublicBySlugAsync_filters_unpublished_posts()
    {
        var (sut, repo) = MakeSut(Array.Empty<string>());
        repo.Setup(x => x.GetBySlugAsync("draft", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CredoCms.Domain.Blog.BlogPost
            {
                Id = Guid.NewGuid(),
                Slug = "draft",
                Title = "Draft",
                BodyJson = "{}",
                IsPublished = false,
                AuthorUserId = Guid.NewGuid(),
                Category = "Devotional",
            });

        var result = await sut.GetPublicBySlugAsync("draft");
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetPublicBySlugAsync_filters_members_only_for_anonymous()
    {
        var (sut, repo) = MakeSut(Array.Empty<string>());
        var anonUser = new Mock<ICurrentUserService>();
        anonUser.SetupGet(x => x.IsAuthenticated).Returns(false);
        anonUser.SetupGet(x => x.UserId).Returns(SystemConstants.SystemUserId);
        anonUser.SetupGet(x => x.Roles).Returns(Array.Empty<string>());

        // Reset IsAuthenticated by re-creating sut with anonymous user.
        var store = new Mock<IUserStore<ApplicationUser>>();
        var um = new Mock<UserManager<ApplicationUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        um.Setup(x => x.FindByIdAsync(It.IsAny<string>()))
            .ReturnsAsync(new ApplicationUser { FirstName = "Author", LastName = "Person" });

        var sut2 = new BlogService(
            repo.Object, um.Object, Mock.Of<ITagService>(), Mock.Of<ITagRepository>(),
            anonUser.Object, Mock.Of<IAuditLogger>(), Mock.Of<IOutputCacheInvalidator>(),
            new CreateBlogPostRequestValidator(), new UpdateBlogPostRequestValidator());

        repo.Setup(x => x.GetBySlugAsync("members-only", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CredoCms.Domain.Blog.BlogPost
            {
                Id = Guid.NewGuid(),
                Slug = "members-only",
                Title = "T",
                BodyJson = "{}",
                IsPublished = true,
                IsMembersOnly = true,
                PublishedAt = DateTimeOffset.UtcNow.AddDays(-1),
                AuthorUserId = Guid.NewGuid(),
                Category = "Devotional",
            });

        var result = await sut2.GetPublicBySlugAsync("members-only");

        result.Should().BeNull();
    }
}
