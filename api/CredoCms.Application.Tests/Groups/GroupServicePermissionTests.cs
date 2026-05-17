using CredoCms.Application.Caching;
using CredoCms.Application.Common;
using CredoCms.Application.Groups;
using CredoCms.Application.RealTime;
using CredoCms.Domain.Common;
using CredoCms.Domain.Groups;
using CredoCms.Domain.Identity;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace CredoCms.Application.Tests.Groups;

/// <summary>
/// Permission-rule coverage for <see cref="GroupService"/>. The full
/// integration flows (request → notify → approve → notify) are exercised in
/// Api.Tests; here we focus on the role gates the service owns:
///   • Administrator-only on create / update / delete / promote-to-leader
///   • Editor + Administrator on add-member / remove-member
///   • Active leader on approve / decline / remove
///   • Authenticated user on submit-request / leave
///   • Visibility gate on public reads (Hidden → null, MembersOnly → null
///     for anonymous)
/// </summary>
public sealed class GroupServicePermissionTests
{
    private static (GroupService sut,
        Mock<IGroupRepository> groups,
        Mock<IGroupMembershipRepository> memberships,
        Mock<ICurrentUserService> user,
        Mock<IRealtimeNotifier> notifier) MakeSut(
        params string[] roles)
    {
        var groupRepo = new Mock<IGroupRepository>();
        var membershipRepo = new Mock<IGroupMembershipRepository>();

        var store = new Mock<IUserStore<ApplicationUser>>();
        var um = new Mock<UserManager<ApplicationUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        um.Setup(x => x.FindByIdAsync(It.IsAny<string>()))
            .ReturnsAsync((ApplicationUser?)null);

        var userId = Guid.NewGuid();
        var userMock = new Mock<ICurrentUserService>();
        userMock.SetupGet(x => x.UserId).Returns(userId);
        userMock.SetupGet(x => x.DisplayName).Returns("Test User");
        userMock.SetupGet(x => x.IsAuthenticated).Returns(true);
        userMock.SetupGet(x => x.Roles).Returns(roles);

        var notifier = new Mock<IRealtimeNotifier>();
        var audit = new Mock<IAuditLogger>();
        var cache = new Mock<IOutputCacheInvalidator>();

        var sut = new GroupService(
            groupRepo.Object, membershipRepo.Object, um.Object,
            userMock.Object, notifier.Object, audit.Object, cache.Object,
            new CreateGroupRequestValidator(), new UpdateGroupRequestValidator());

        return (sut, groupRepo, membershipRepo, userMock, notifier);
    }

    private static CreateGroupRequest ValidCreate(string slug = "youth") => new(
        slug, "Youth Group", null, null, null, null, null, null,
        GroupVisibility.Public, GroupJoinability.Open,
        MessageOnJoinRequest.Optional, RosterVisibility.LeadersOnly, true);

    [Fact]
    public async Task CreateAsync_rejects_non_administrator()
    {
        var (sut, _, _, _, _) = MakeSut(SystemConstants.Roles.Editor);
        var result = await sut.CreateAsync(ValidCreate());
        result.Succeeded.Should().BeFalse();
        result.Errors[0].Should().Contain("administrators");
    }

    [Fact]
    public async Task CreateAsync_succeeds_for_administrator()
    {
        var (sut, groups, _, _, _) = MakeSut(SystemConstants.Roles.Administrator);
        groups.Setup(x => x.SlugExistsAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var result = await sut.CreateAsync(ValidCreate());
        result.Succeeded.Should().BeTrue();
        groups.Verify(x => x.AddAsync(It.IsAny<Group>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetLeaderAsync_rejects_editor()
    {
        var (sut, _, _, _, _) = MakeSut(SystemConstants.Roles.Editor);
        var result = await sut.SetLeaderAsync(Guid.NewGuid(), Guid.NewGuid(), true);
        result.Succeeded.Should().BeFalse();
        result.Errors[0].Should().Contain("administrators");
    }

    [Fact]
    public async Task SetLeaderAsync_succeeds_for_administrator_when_member_active()
    {
        var (sut, _, memberships, _, _) = MakeSut(SystemConstants.Roles.Administrator);
        var groupId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        memberships.Setup(x => x.GetLiveMembershipAsync(groupId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GroupMembership { Id = Guid.NewGuid(), GroupId = groupId, UserId = userId, Status = GroupMembershipStatus.Active });

        var result = await sut.SetLeaderAsync(groupId, userId, true);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task ApproveJoinRequestAsync_rejects_uninvolved_member()
    {
        // Plain "Member" role with no leadership in this group → cannot approve.
        var (sut, _, memberships, user, _) = MakeSut(SystemConstants.Roles.Member);
        var groupId = Guid.NewGuid();
        var membershipId = Guid.NewGuid();
        memberships.Setup(x => x.GetAsync(membershipId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GroupMembership
            {
                Id = membershipId,
                GroupId = groupId,
                Status = GroupMembershipStatus.Pending,
            });
        memberships.Setup(x => x.GetLiveMembershipAsync(groupId, user.Object.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((GroupMembership?)null);

        var result = await sut.ApproveJoinRequestAsync(membershipId);

        result.Succeeded.Should().BeFalse();
        result.Errors[0].Should().Contain("permission");
    }

    [Fact]
    public async Task ApproveJoinRequestAsync_succeeds_for_active_leader()
    {
        var (sut, groups, memberships, user, notifier) = MakeSut(SystemConstants.Roles.Member);
        var groupId = Guid.NewGuid();
        var membershipId = Guid.NewGuid();
        var requesterId = Guid.NewGuid();

        memberships.Setup(x => x.GetAsync(membershipId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GroupMembership
            {
                Id = membershipId,
                GroupId = groupId,
                UserId = requesterId,
                Status = GroupMembershipStatus.Pending,
            });
        // Caller has active leader membership in this group.
        memberships.Setup(x => x.GetLiveMembershipAsync(groupId, user.Object.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GroupMembership
            {
                Id = Guid.NewGuid(),
                GroupId = groupId,
                UserId = user.Object.UserId,
                Status = GroupMembershipStatus.Active,
                IsLeader = true,
            });
        groups.Setup(x => x.GetAsync(groupId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Group { Id = groupId, Name = "Youth", Slug = "youth" });

        var result = await sut.ApproveJoinRequestAsync(membershipId);

        result.Succeeded.Should().BeTrue();
        notifier.Verify(n => n.NotifyGroupMembershipDecisionAsync(
            requesterId,
            It.Is<GroupMembershipDecisionMessage>(m => m.Approved),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetPublicBySlugAsync_returns_null_for_hidden_groups()
    {
        var (sut, groups, _, _, _) = MakeSut(SystemConstants.Roles.Administrator);
        groups.Setup(x => x.GetBySlugAsync("hidden", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Group
            {
                Id = Guid.NewGuid(),
                Slug = "hidden",
                Name = "Secret",
                IsActive = true,
                Visibility = GroupVisibility.Hidden,
            });

        var result = await sut.GetPublicBySlugAsync("hidden");

        // Hidden never surfaces through the public endpoint, even to admins.
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetPublicBySlugAsync_returns_null_for_members_only_when_anonymous()
    {
        var groupRepo = new Mock<IGroupRepository>();
        var memberships = new Mock<IGroupMembershipRepository>();
        var store = new Mock<IUserStore<ApplicationUser>>();
        var um = new Mock<UserManager<ApplicationUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        // Anonymous: IsAuthenticated=false, UserId=System.
        var user = new Mock<ICurrentUserService>();
        user.SetupGet(x => x.IsAuthenticated).Returns(false);
        user.SetupGet(x => x.UserId).Returns(SystemConstants.SystemUserId);
        user.SetupGet(x => x.Roles).Returns(Array.Empty<string>());

        groupRepo.Setup(x => x.GetBySlugAsync("staff", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Group
            {
                Id = Guid.NewGuid(),
                Slug = "staff",
                Name = "Staff",
                IsActive = true,
                Visibility = GroupVisibility.MembersOnly,
            });

        var sut = new GroupService(
            groupRepo.Object, memberships.Object, um.Object,
            user.Object, Mock.Of<IRealtimeNotifier>(), Mock.Of<IAuditLogger>(),
            Mock.Of<IOutputCacheInvalidator>(),
            new CreateGroupRequestValidator(), new UpdateGroupRequestValidator());

        var result = await sut.GetPublicBySlugAsync("staff");

        result.Should().BeNull();
    }

    [Fact]
    public async Task SubmitJoinRequestAsync_rejects_closed_group()
    {
        var (sut, groups, _, _, _) = MakeSut(SystemConstants.Roles.Member);
        groups.Setup(x => x.GetBySlugAsync("closed", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Group
            {
                Id = Guid.NewGuid(),
                Slug = "closed",
                Name = "Closed",
                IsActive = true,
                Visibility = GroupVisibility.Public,
                Joinability = GroupJoinability.Closed,
            });

        var result = await sut.SubmitJoinRequestAsync("closed", new JoinRequestRequest(null));

        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task SubmitJoinRequestAsync_requires_message_when_required_by_group()
    {
        var (sut, groups, _, _, _) = MakeSut(SystemConstants.Roles.Member);
        groups.Setup(x => x.GetBySlugAsync("youth", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Group
            {
                Id = Guid.NewGuid(),
                Slug = "youth",
                Name = "Youth",
                IsActive = true,
                Visibility = GroupVisibility.Public,
                Joinability = GroupJoinability.Open,
                RequiresMessageOnJoinRequest = MessageOnJoinRequest.Required,
            });

        var result = await sut.SubmitJoinRequestAsync("youth", new JoinRequestRequest(null));

        result.Succeeded.Should().BeFalse();
        result.Errors[0].Should().Contain("message");
    }

    [Fact]
    public async Task SubmitJoinRequestAsync_emits_signalr_event_and_notifies_leaders()
    {
        var (sut, groups, memberships, _, notifier) = MakeSut(SystemConstants.Roles.Member);
        var groupId = Guid.NewGuid();
        var leader1 = Guid.NewGuid();
        var leader2 = Guid.NewGuid();
        groups.Setup(x => x.GetBySlugAsync("youth", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Group
            {
                Id = groupId,
                Slug = "youth",
                Name = "Youth",
                IsActive = true,
                Visibility = GroupVisibility.Public,
                Joinability = GroupJoinability.Open,
            });
        memberships.Setup(x => x.GetLiveMembershipAsync(groupId, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GroupMembership?)null);
        memberships.Setup(x => x.ListLeaderUserIdsAsync(groupId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Guid> { leader1, leader2 });

        var result = await sut.SubmitJoinRequestAsync("youth", new JoinRequestRequest("Hi"));

        result.Succeeded.Should().BeTrue();
        notifier.Verify(n => n.NotifyGroupJoinRequestSubmittedAsync(
            It.Is<GroupJoinRequestMessage>(m => m.GroupId == groupId),
            It.Is<IReadOnlyCollection<Guid>>(ids => ids.Count == 2 && ids.Contains(leader1) && ids.Contains(leader2)),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
