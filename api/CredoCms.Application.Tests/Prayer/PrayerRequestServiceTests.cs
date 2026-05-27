using CredoCms.Application.Common;
using CredoCms.Application.Prayer;
using CredoCms.Application.Profanity;
using CredoCms.Application.RealTime;
using CredoCms.Application.SiteSettingsManagement;
using CredoCms.Domain.Common;
using CredoCms.Domain.Identity;
using CredoCms.Domain.Prayer;
using CredoCms.Domain.Settings;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace CredoCms.Application.Tests.Prayer;

/// <summary>
/// Permission + privacy contract for PrayerRequestService:
///   • cross-user edits denied
///   • profanity blocks submit and edit
///   • anonymous requests hide submitter from non-privileged viewers but
///     keep them visible to admins/editors and to the submitter
///   • Editor can post update; plain Member cannot
///   • mark/unmark idempotent (Add returning false on second call)
/// </summary>
public sealed class PrayerRequestServiceTests
{
    private static (
        PrayerRequestService sut,
        Mock<IPrayerRequestRepository> repo,
        Mock<ICurrentUserService> user,
        Mock<IProfanityCheckService> profanity,
        Mock<IRealtimeNotifier> notifier,
        Guid callerId)
        MakeSut(string[] roles, Guid? userId = null)
    {
        var repo = new Mock<IPrayerRequestRepository>();
        var profanity = new Mock<IProfanityCheckService>();
        profanity.Setup(x => x.ContainsProfanityAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var settings = new Mock<ISiteSettingsRepository>();
        settings.Setup(x => x.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SiteSettings { PrayerRequestArchiveDays = 30 });

        var store = new Mock<IUserStore<ApplicationUser>>();
        var um = new Mock<UserManager<ApplicationUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        um.Setup(x => x.FindByIdAsync(It.IsAny<string>()))
            .ReturnsAsync((string id) =>
                new ApplicationUser { Id = Guid.Parse(id), FirstName = "User", LastName = id[..4] });

        var caller = userId ?? Guid.NewGuid();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.SetupGet(x => x.UserId).Returns(caller);
        currentUser.SetupGet(x => x.DisplayName).Returns("Test User");
        currentUser.SetupGet(x => x.IsAuthenticated).Returns(roles.Length > 0);
        currentUser.SetupGet(x => x.Roles).Returns(roles);

        var notifier = new Mock<IRealtimeNotifier>();
        var audit = new Mock<IAuditLogger>();

        var sut = new PrayerRequestService(
            repo.Object, um.Object, currentUser.Object, profanity.Object,
            settings.Object, notifier.Object, audit.Object,
            new SubmitPrayerRequestRequestValidator(),
            new EditPrayerRequestRequestValidator(),
            new AddPrayerUpdateRequestValidator());

        return (sut, repo, currentUser, profanity, notifier, caller);
    }

    [Fact]
    public async Task SubmitAsync_blocks_when_profanity_detected_in_title()
    {
        var (sut, _, _, profanity, _, _) = MakeSut(new[] { SystemConstants.Roles.Member });
        profanity.Setup(x => x.ContainsProfanityAsync("Bad title", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await sut.SubmitAsync(new SubmitPrayerRequestRequest("Bad title", "{}", false));

        result.Succeeded.Should().BeFalse();
        result.Errors[0].Should().Contain("revise");
    }

    [Fact]
    public async Task SubmitAsync_persists_and_emits_signalr()
    {
        var (sut, repo, _, _, notifier, callerId) = MakeSut(new[] { SystemConstants.Roles.Member });
        repo.Setup(x => x.PrayedForCountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(0);
        repo.Setup(x => x.ListUpdatesForAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PrayerRequestUpdate>());

        var result = await sut.SubmitAsync(new SubmitPrayerRequestRequest("Healing", "{\"text\":\"please pray\"}", false));

        result.Succeeded.Should().BeTrue();
        repo.Verify(x => x.AddAsync(
            It.Is<PrayerRequest>(r => r.SubmittedByUserId == callerId
                && r.Title == "Healing"
                && r.Status == PrayerRequestStatus.Active),
            It.IsAny<CancellationToken>()), Times.Once);
        notifier.Verify(x => x.NotifyPrayerRequestEventAsync(
            It.Is<PrayerRequestEventMessage>(m => m.Kind == "PrayerRequestCreated"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EditAsync_denies_cross_user_edit()
    {
        var (sut, repo, _, _, _, callerId) = MakeSut(new[] { SystemConstants.Roles.Member });
        var someoneElse = Guid.NewGuid();
        repo.Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PrayerRequest
            {
                Id = Guid.NewGuid(),
                Title = "Existing",
                BodyJson = "{}",
                SubmittedByUserId = someoneElse,
            });

        var result = await sut.EditAsync(Guid.NewGuid(),
            new EditPrayerRequestRequest("New title", "{}", false));

        result.Succeeded.Should().BeFalse();
        result.Errors[0].Should().Contain("permission");
    }

    [Fact]
    public async Task EditAsync_allows_admin_to_edit_other_users_request()
    {
        var (sut, repo, _, _, _, _) = MakeSut(new[] { SystemConstants.Roles.Administrator });
        var someoneElse = Guid.NewGuid();
        var entity = new PrayerRequest
        {
            Id = Guid.NewGuid(),
            Title = "Existing",
            BodyJson = "{}",
            SubmittedByUserId = someoneElse,
        };
        repo.Setup(x => x.GetAsync(entity.Id, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        repo.Setup(x => x.PrayedForCountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(0);
        repo.Setup(x => x.ListUpdatesForAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PrayerRequestUpdate>());

        var result = await sut.EditAsync(entity.Id,
            new EditPrayerRequestRequest("Edited", "{}", false));

        result.Succeeded.Should().BeTrue();
        entity.Title.Should().Be("Edited");
    }

    [Fact]
    public async Task GetMemberAsync_hides_submitter_when_anonymous_and_viewer_is_other_member()
    {
        var (sut, repo, _, _, _, _) = MakeSut(new[] { SystemConstants.Roles.Member });
        var submitter = Guid.NewGuid();
        repo.Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PrayerRequest
            {
                Id = Guid.NewGuid(),
                Title = "T",
                BodyJson = "{}",
                IsAnonymous = true,
                SubmittedByUserId = submitter,
            });
        repo.Setup(x => x.PrayedForCountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(0);
        repo.Setup(x => x.HasPrayedAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        repo.Setup(x => x.ListUpdatesForAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PrayerRequestUpdate>());

        var result = await sut.GetMemberAsync(Guid.NewGuid());

        result.Should().NotBeNull();
        result!.SubmitterDisplayName.Should().BeNull();
    }

    [Fact]
    public async Task GetMemberAsync_shows_submitter_to_admin_even_when_anonymous()
    {
        var (sut, repo, _, _, _, _) = MakeSut(new[] { SystemConstants.Roles.Administrator });
        repo.Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PrayerRequest
            {
                Id = Guid.NewGuid(),
                Title = "T",
                BodyJson = "{}",
                IsAnonymous = true,
                SubmittedByUserId = Guid.NewGuid(),
            });
        repo.Setup(x => x.PrayedForCountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(0);
        repo.Setup(x => x.HasPrayedAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        repo.Setup(x => x.ListUpdatesForAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PrayerRequestUpdate>());

        var result = await sut.GetMemberAsync(Guid.NewGuid());

        result!.SubmitterDisplayName.Should().NotBeNull();
    }

    [Fact]
    public async Task AddUpdateAsync_blocks_member_from_posting()
    {
        var (sut, repo, _, _, _, _) = MakeSut(new[] { SystemConstants.Roles.Member });
        repo.Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PrayerRequest { Id = Guid.NewGuid(), Title = "T", BodyJson = "{}" });

        var result = await sut.AddUpdateAsync(Guid.NewGuid(),
            new AddPrayerUpdateRequest("{\"text\":\"praise report\"}"));

        result.Succeeded.Should().BeFalse();
        result.Errors[0].Should().Contain("editors");
    }

    [Fact]
    public async Task AddUpdateAsync_allows_editor_to_post()
    {
        var (sut, repo, _, _, notifier, _) = MakeSut(new[] { SystemConstants.Roles.Editor });
        var entity = new PrayerRequest { Id = Guid.NewGuid(), Title = "T", BodyJson = "{}" };
        repo.Setup(x => x.GetAsync(entity.Id, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        repo.Setup(x => x.PrayedForCountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(0);
        repo.Setup(x => x.ListUpdatesForAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PrayerRequestUpdate>());

        var result = await sut.AddUpdateAsync(entity.Id,
            new AddPrayerUpdateRequest("{\"text\":\"praise report\"}"));

        result.Succeeded.Should().BeTrue();
        repo.Verify(x => x.AddUpdateAsync(It.IsAny<PrayerRequestUpdate>(), It.IsAny<CancellationToken>()), Times.Once);
        notifier.Verify(x => x.NotifyPrayerRequestEventAsync(
            It.Is<PrayerRequestEventMessage>(m => m.Kind == "PrayerRequestUpdateAdded"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MarkPrayedForAsync_is_idempotent()
    {
        var (sut, repo, _, _, _, _) = MakeSut(new[] { SystemConstants.Roles.Member });
        var requestId = Guid.NewGuid();
        repo.Setup(x => x.GetAsync(requestId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PrayerRequest { Id = requestId, Title = "T", BodyJson = "{}" });
        // Second call returns false (no row added) — service should still
        // succeed and return the latest count.
        repo.SetupSequence(x => x.AddPrayedForAsync(requestId, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);
        repo.Setup(x => x.PrayedForCountAsync(requestId, It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var first = await sut.MarkPrayedForAsync(requestId);
        var second = await sut.MarkPrayedForAsync(requestId);

        first.Should().Be(1);
        second.Should().Be(1);
    }
}
