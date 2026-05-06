using CredoCms.Application.Common;
using CredoCms.Application.RealTime;
using CredoCms.Application.Volunteers;
using CredoCms.Domain.Common;
using CredoCms.Domain.Volunteers;
using Moq;

namespace CredoCms.Application.Tests.Volunteers;

public sealed class EventVolunteerServiceTests
{
    private static (
        EventVolunteerService Sut,
        Mock<IEventVolunteerRoleRepository> Roles,
        Mock<IEventVolunteerSignupRepository> Signups,
        Mock<ICurrentUserService> CurrentUser,
        Mock<IRealtimeNotifier> Notifier) MakeSut(string[]? roles = null, Guid? userId = null)
    {
        var roleRepo = new Mock<IEventVolunteerRoleRepository>();
        var signupRepo = new Mock<IEventVolunteerSignupRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.SetupGet(x => x.UserId).Returns(userId ?? Guid.NewGuid());
        currentUser.SetupGet(x => x.IsAuthenticated).Returns(true);
        currentUser.SetupGet(x => x.Roles).Returns(roles ?? new[] { SystemConstants.Roles.Member });
        var notifier = new Mock<IRealtimeNotifier>();
        var audit = new Mock<IAuditLogger>();

        var sut = new EventVolunteerService(
            roleRepo.Object, signupRepo.Object, currentUser.Object, notifier.Object, audit.Object);
        return (sut, roleRepo, signupRepo, currentUser, notifier);
    }

    [Fact]
    public async Task CreateRole_requires_admin_shell()
    {
        var (sut, _, _, _, _) = MakeSut();
        await FluentActions.Invoking(() => sut.CreateRoleAsync(Guid.NewGuid(),
            new CreateRoleRequest("Greeter", null, 2, 0)))
            .Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task SignUp_blocks_when_already_signed_up()
    {
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7));
        var (sut, roles, signups, _, _) = MakeSut(userId: userId);
        roles.Setup(r => r.GetAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EventVolunteerRole { Id = roleId, EventId = Guid.NewGuid(), RoleName = "Greeter", SlotsNeeded = 2 });
        signups.Setup(r => r.ListActiveForRoleOccurrenceAsync(roleId, date, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EventVolunteerSignup> { new() { UserId = userId } });

        await FluentActions.Invoking(() => sut.SignUpAsync(roleId, date))
            .Should().ThrowAsync<InvalidOperationException>().WithMessage("*already*");
    }

    [Fact]
    public async Task SignUp_blocks_when_role_full()
    {
        var (sut, roles, signups, _, _) = MakeSut();
        var roleId = Guid.NewGuid();
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7));
        roles.Setup(r => r.GetAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EventVolunteerRole { Id = roleId, EventId = Guid.NewGuid(), RoleName = "Greeter", SlotsNeeded = 1 });
        signups.Setup(r => r.ListActiveForRoleOccurrenceAsync(roleId, date, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EventVolunteerSignup> { new() { UserId = Guid.NewGuid() } });

        await FluentActions.Invoking(() => sut.SignUpAsync(roleId, date))
            .Should().ThrowAsync<InvalidOperationException>().WithMessage("*full*");
    }

    [Fact]
    public async Task SignUp_emits_VolunteerSlotFilled_signal()
    {
        var (sut, roles, signups, _, notifier) = MakeSut();
        var roleId = Guid.NewGuid();
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7));
        roles.Setup(r => r.GetAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EventVolunteerRole { Id = roleId, EventId = Guid.NewGuid(), RoleName = "Greeter", SlotsNeeded = 2 });
        signups.Setup(r => r.ListActiveForRoleOccurrenceAsync(roleId, date, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EventVolunteerSignup>());

        await sut.SignUpAsync(roleId, date);

        notifier.Verify(n => n.NotifyVolunteerSlotAsync(
            It.Is<VolunteerSlotMessage>(m => m.Kind == "VolunteerSlotFilled"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Cancel_emits_VolunteerSlotOpened_signal()
    {
        var userId = Guid.NewGuid();
        var (sut, roles, signups, _, notifier) = MakeSut(userId: userId);
        var roleId = Guid.NewGuid();
        var signupId = Guid.NewGuid();
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7));
        signups.Setup(r => r.GetAsync(signupId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EventVolunteerSignup
            { Id = signupId, UserId = userId, EventVolunteerRoleId = roleId, OccurrenceDate = date, EventId = Guid.NewGuid() });
        roles.Setup(r => r.GetAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EventVolunteerRole { Id = roleId, EventId = Guid.NewGuid(), RoleName = "Greeter", SlotsNeeded = 2 });
        signups.Setup(r => r.ListActiveForRoleOccurrenceAsync(roleId, date, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EventVolunteerSignup>());

        await sut.CancelSignupAsync(signupId);

        notifier.Verify(n => n.NotifyVolunteerSlotAsync(
            It.Is<VolunteerSlotMessage>(m => m.Kind == "VolunteerSlotOpened"),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
