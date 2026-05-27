using CredoCms.Application.Email;
using CredoCms.Application.SiteSettingsManagement;
using CredoCms.Domain.Blog;
using CredoCms.Domain.Email;
using CredoCms.Domain.News;
using CredoCms.Domain.Settings;
using Moq;

namespace CredoCms.Application.Tests.Email;

public sealed class EmailOnPublishServiceTests
{
    private static (EmailOnPublishService Sut, Mock<IEmailBroadcastRepository> Broadcasts) MakeSut(SiteSettings? overrides = null)
    {
        var broadcasts = new Mock<IEmailBroadcastRepository>();
        var settings = new Mock<ISiteSettingsRepository>();
        settings.Setup(s => s.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(overrides ?? new SiteSettings
            {
                ChurchName = "Hope",
                NewsEmailTargetMode = BroadcastTargetMode.AllMembers,
                BlogEmailTargetMode = BroadcastTargetMode.AllMembers,
                EmailSubjectPrefixNews = "[News]",
                EmailSubjectPrefixBlog = "[Blog]",
            });
        return (new EmailOnPublishService(broadcasts.Object, settings.Object), broadcasts);
    }

    [Fact]
    public async Task News_returns_null_when_flag_off()
    {
        var (sut, broadcasts) = MakeSut();
        var result = await sut.OnNewsPublishedAsync(new NewsItem { IsPublished = true, SendEmailOnPublish = false });
        result.Should().BeNull();
        broadcasts.Verify(b => b.AddAsync(It.IsAny<EmailBroadcast>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task News_creates_sending_broadcast_when_flag_on()
    {
        EmailBroadcast? captured = null;
        var (sut, broadcasts) = MakeSut();
        broadcasts.Setup(b => b.AddAsync(It.IsAny<EmailBroadcast>(), It.IsAny<CancellationToken>()))
            .Callback<EmailBroadcast, CancellationToken>((b, _) => captured = b)
            .Returns(Task.CompletedTask);

        var result = await sut.OnNewsPublishedAsync(new NewsItem
        {
            Id = Guid.NewGuid(),
            Slug = "welcome",
            Title = "Welcome",
            Excerpt = "Hi friends",
            IsPublished = true,
            SendEmailOnPublish = true,
        });

        result.Should().NotBeNull();
        captured.Should().NotBeNull();
        captured!.Status.Should().Be(BroadcastStatus.Sending);
        captured.Category.Should().Be(EmailCategory.News);
        captured.Subject.Should().Be("[News] Welcome");
        captured.Body.Should().Contain("Welcome");
        captured.SourceEntityId.Should().NotBeNull();
    }

    [Fact]
    public async Task Blog_uses_blog_subject_prefix_and_category()
    {
        EmailBroadcast? captured = null;
        var (sut, broadcasts) = MakeSut();
        broadcasts.Setup(b => b.AddAsync(It.IsAny<EmailBroadcast>(), It.IsAny<CancellationToken>()))
            .Callback<EmailBroadcast, CancellationToken>((b, _) => captured = b)
            .Returns(Task.CompletedTask);

        await sut.OnBlogPublishedAsync(new BlogPost
        {
            Id = Guid.NewGuid(),
            Slug = "first-post",
            Title = "First post",
            BodyJson = "{}",
            Category = "Devotional",
            IsPublished = true,
            SendEmailOnPublish = true,
        });

        captured.Should().NotBeNull();
        captured!.Category.Should().Be(EmailCategory.Blog);
        captured.Subject.Should().Be("[Blog] First post");
    }

    [Fact]
    public async Task News_returns_null_when_not_yet_published()
    {
        var (sut, broadcasts) = MakeSut();
        var result = await sut.OnNewsPublishedAsync(new NewsItem { IsPublished = false, SendEmailOnPublish = true });
        result.Should().BeNull();
        broadcasts.Verify(b => b.AddAsync(It.IsAny<EmailBroadcast>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
