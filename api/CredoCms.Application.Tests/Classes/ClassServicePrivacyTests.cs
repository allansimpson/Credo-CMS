using CredoCms.Application.Caching;
using CredoCms.Application.Classes;
using CredoCms.Application.Common;
using CredoCms.Application.Leaders;
using CredoCms.Domain.Classes;
using CredoCms.Domain.Common;
using Moq;

namespace CredoCms.Application.Tests.Classes;

/// <summary>
/// The privacy contract for the public classes endpoint: when the caller is
/// anonymous, member-only fields (DefaultRoom, TeacherLeaderId, TeacherFreeText,
/// DetailedScheduleJson, MaterialsNeeded) must be absent from the response.
/// The service achieves this by emitting two separate DTO shapes — public-safe
/// vs member-augmented — rather than nulling fields on a shared shape.
/// </summary>
public sealed class ClassServicePrivacyTests
{
    private static (ClassService sut,
        Mock<IClassSlotRepository> slots,
        Mock<IClassOfferingRepository> offerings,
        Mock<ILeaderRepository> leaders) MakeSut(params string[] roles)
    {
        var slotRepo = new Mock<IClassSlotRepository>();
        var offeringRepo = new Mock<IClassOfferingRepository>();
        var leaderRepo = new Mock<ILeaderRepository>();

        var user = new Mock<ICurrentUserService>();
        user.SetupGet(x => x.UserId).Returns(SystemConstants.SystemUserId);
        user.SetupGet(x => x.IsAuthenticated).Returns(roles.Length > 0);
        user.SetupGet(x => x.Roles).Returns(roles);

        var sut = new ClassService(
            slotRepo.Object, offeringRepo.Object, leaderRepo.Object,
            user.Object, Mock.Of<IAuditLogger>(), Mock.Of<IOutputCacheInvalidator>(),
            new CreateClassSlotRequestValidator(),
            new UpdateClassSlotRequestValidator(),
            new CreateClassOfferingRequestValidator(),
            new UpdateClassOfferingRequestValidator());
        return (sut, slotRepo, offeringRepo, leaderRepo);
    }

    private static ClassSlot MakeSlot() => new()
    {
        Id = Guid.NewGuid(),
        Slug = "adults",
        Name = "Adult Class",
        AudienceAgeGroup = "Adults",
        DefaultRoom = "Room 101",
        IsActive = true,
    };

    private static ClassOffering MakeOffering(Guid slotId) => new()
    {
        Id = Guid.NewGuid(),
        ClassSlotId = slotId,
        Subject = "Romans",
        StartDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-7),
        EndDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(35),
        TeacherLeaderId = Guid.NewGuid(),
        TeacherFreeText = "Pastor James",
        DetailedScheduleJson = "{\"weeks\":[]}",
        MaterialsNeeded = "Bible, notebook",
    };

    [Fact]
    public async Task Public_list_uses_public_safe_dto_with_no_member_fields()
    {
        var (sut, slots, offerings, _) = MakeSut(); // anonymous
        var slot = MakeSlot();
        var offering = MakeOffering(slot.Id);

        slots.Setup(x => x.ListPublicAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ClassSlot> { slot });
        offerings.Setup(x => x.GetCurrentForSlotAsync(slot.Id, It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(offering);

        var result = await sut.ListPublicAsync(showRecentPast: false, recentPastLookbackDays: 30);

        var slotDto = result.Single();
        // PublicClassSlotDto has no DefaultRoom field by design (compile-time
        // proof; this assertion just exercises the call site).
        slotDto.Should().BeOfType<PublicClassSlotDto>();
        slotDto.CurrentOffering.Should().NotBeNull();
        // PublicClassOfferingDto only carries Subject + dates + description.
        slotDto.CurrentOffering!.GetType().GetProperty("TeacherLeaderId").Should().BeNull();
        slotDto.CurrentOffering.GetType().GetProperty("MaterialsNeeded").Should().BeNull();
    }

    [Fact]
    public async Task Member_list_includes_member_only_fields()
    {
        var (sut, slots, offerings, leaders) = MakeSut(SystemConstants.Roles.Member);
        var slot = MakeSlot();
        var offering = MakeOffering(slot.Id);

        slots.Setup(x => x.ListPublicAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ClassSlot> { slot });
        offerings.Setup(x => x.GetCurrentForSlotAsync(slot.Id, It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(offering);
        leaders.Setup(x => x.GetByIdAsync(offering.TeacherLeaderId!.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CredoCms.Domain.Leaders.Leader { Id = offering.TeacherLeaderId!.Value, FullName = "Pastor James" });

        var result = await sut.ListMemberAsync(showRecentPast: false, recentPastLookbackDays: 30);

        var slotDto = result.Single();
        slotDto.DefaultRoom.Should().Be("Room 101");
        slotDto.CurrentOffering.Should().NotBeNull();
        slotDto.CurrentOffering!.TeacherLeaderId.Should().NotBeNull();
        slotDto.CurrentOffering.TeacherLeaderId!.Value.Should().Be(offering.TeacherLeaderId!.Value);
        slotDto.CurrentOffering.TeacherLeaderName.Should().Be("Pastor James");
        slotDto.CurrentOffering.TeacherFreeText.Should().Be("Pastor James");
        slotDto.CurrentOffering.DetailedScheduleJson.Should().Be("{\"weeks\":[]}");
        slotDto.CurrentOffering.MaterialsNeeded.Should().Be("Bible, notebook");
    }

    [Fact]
    public async Task Public_get_returns_null_for_inactive_slot()
    {
        var (sut, slots, _, _) = MakeSut();
        var slot = MakeSlot();
        slot.IsActive = false;
        slots.Setup(x => x.GetBySlugAsync("adults", It.IsAny<CancellationToken>()))
            .ReturnsAsync(slot);

        var result = await sut.GetPublicBySlugAsync("adults", showRecentPast: false, recentPastLookbackDays: 30);

        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateSlotAsync_rejects_non_administrator()
    {
        var (sut, _, _, _) = MakeSut(SystemConstants.Roles.Editor);
        var result = await sut.CreateSlotAsync(new CreateClassSlotRequest(
            "adults", "Adults", "Adults", null, null, null, null, null, null, true, 0));
        result.Succeeded.Should().BeFalse();
        result.Errors[0].Should().Contain("administrators");
    }

    [Fact]
    public async Task UpdateOfferingAsync_validates_end_date_after_start_date()
    {
        var (sut, slots, offerings, _) = MakeSut(SystemConstants.Roles.Administrator);
        var slot = MakeSlot();
        var offering = MakeOffering(slot.Id);
        offerings.Setup(x => x.GetAsync(offering.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(offering);
        slots.Setup(x => x.GetAsync(slot.Id, It.IsAny<CancellationToken>())).ReturnsAsync(slot);

        var start = DateOnly.FromDateTime(DateTime.UtcNow);
        var result = await sut.UpdateOfferingAsync(offering.Id, new UpdateClassOfferingRequest(
            slot.Id, "Romans", null,
            StartDate: start.AddDays(10),
            EndDate: start, // end before start — should fail
            null, null, null, null));

        result.Succeeded.Should().BeFalse();
    }
}
