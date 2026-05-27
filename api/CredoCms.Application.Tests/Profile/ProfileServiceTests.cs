using CredoCms.Application.Common;
using CredoCms.Application.Profile;
using CredoCms.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace CredoCms.Application.Tests.Profile;

/// <summary>
/// Service-layer tests for the four profile mutators. We use a light shim
/// around <see cref="UserManager{TUser}"/> rather than full integration plumbing
/// — the goal is to lock down the rules the service actually owns:
///   • caller's user id is the only id that can be touched
///   • directory master toggle gates per-field toggles
///   • photo without alt text is rejected at the service layer
///   • each mutator writes an audit entry
/// </summary>
public sealed class ProfileServiceTests
{
    private static (ProfileService sut, Mock<IAuditLogger> audit, ApplicationUser user) MakeSut(
        ApplicationUser? seedUser = null)
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        var um = new Mock<UserManager<ApplicationUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        var user = seedUser ?? new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "alice@example.com",
            Email = "alice@example.com",
            FirstName = "Alice",
            LastName = "Adams",
        };

        um.Setup(x => x.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        um.Setup(x => x.UpdateAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(IdentityResult.Success);

        var audit = new Mock<IAuditLogger>();
        var sut = new ProfileService(um.Object, audit.Object);
        return (sut, audit, user);
    }

    [Fact]
    public async Task GetProfileAsync_returns_null_when_user_missing()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        var um = new Mock<UserManager<ApplicationUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        um.Setup(x => x.FindByIdAsync(It.IsAny<string>()))
            .ReturnsAsync((ApplicationUser?)null);
        var sut = new ProfileService(um.Object, Mock.Of<IAuditLogger>());

        var result = await sut.GetProfileAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdatePersonalInfoAsync_persists_fields_and_audits()
    {
        var (sut, audit, user) = MakeSut();

        var result = await sut.UpdatePersonalInfoAsync(user.Id, new UpdatePersonalInfoRequest(
            PhoneNumber: "555-0100",
            AddressLine1: "1 Main",
            AddressLine2: null,
            City: "Springfield",
            StateOrRegion: "OR",
            PostalCode: "97000",
            Country: "USA",
            PhotoBlobUrl: null,
            PhotoWebpBlobUrl: null,
            PhotoAltText: null,
            PublicAuthorBio: null));

        result.Succeeded.Should().BeTrue();
        result.Profile!.PhoneNumber.Should().Be("555-0100");
        user.City.Should().Be("Springfield");
        audit.Verify(a => a.WriteAsync(
            "Profile.PersonalInfoUpdated",
            "ApplicationUser",
            user.Id.ToString(),
            It.IsAny<object>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdatePersonalInfoAsync_rejects_photo_without_alt_text()
    {
        var (sut, _, user) = MakeSut();

        var result = await sut.UpdatePersonalInfoAsync(user.Id, new UpdatePersonalInfoRequest(
            PhoneNumber: null,
            AddressLine1: null, AddressLine2: null, City: null, StateOrRegion: null,
            PostalCode: null, Country: null,
            PhotoBlobUrl: "https://example.com/photo.jpg",
            PhotoWebpBlobUrl: "https://example.com/photo.webp",
            PhotoAltText: "  ",
            PublicAuthorBio: null));

        result.Succeeded.Should().BeFalse();
        result.Errors[0].Should().Contain("Alt text is required");
    }

    [Fact]
    public async Task UpdateDirectoryAsync_forces_per_field_toggles_off_when_master_off()
    {
        var (sut, _, user) = MakeSut();

        // Caller submits all-on, but master is off — service must collapse them.
        var result = await sut.UpdateDirectoryAsync(user.Id, new UpdateDirectoryRequest(
            IsListedInDirectory: false,
            ShowEmailInDirectory: true,
            ShowPhoneInDirectory: true,
            ShowAddressInDirectory: true,
            ShowPhotoInDirectory: true));

        result.Succeeded.Should().BeTrue();
        user.IsListedInDirectory.Should().BeFalse();
        user.ShowEmailInDirectory.Should().BeFalse();
        user.ShowPhoneInDirectory.Should().BeFalse();
        user.ShowAddressInDirectory.Should().BeFalse();
        user.ShowPhotoInDirectory.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateDirectoryAsync_keeps_per_field_toggles_when_master_on()
    {
        var (sut, _, user) = MakeSut();

        var result = await sut.UpdateDirectoryAsync(user.Id, new UpdateDirectoryRequest(
            IsListedInDirectory: true,
            ShowEmailInDirectory: true,
            ShowPhoneInDirectory: false,
            ShowAddressInDirectory: true,
            ShowPhotoInDirectory: false));

        result.Succeeded.Should().BeTrue();
        user.IsListedInDirectory.Should().BeTrue();
        user.ShowEmailInDirectory.Should().BeTrue();
        user.ShowPhoneInDirectory.Should().BeFalse();
        user.ShowAddressInDirectory.Should().BeTrue();
        user.ShowPhotoInDirectory.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateNotificationsAsync_persists_all_four_flags()
    {
        var (sut, _, user) = MakeSut();

        var result = await sut.UpdateNotificationsAsync(user.Id, new UpdateNotificationsRequest(
            ReceiveNewsEmails: false,
            ReceiveBlogEmails: true,
            ReceiveBroadcastEmails: false,
            ReceiveGroupEmailsGlobal: false));

        result.Succeeded.Should().BeTrue();
        user.ReceiveNewsEmails.Should().BeFalse();
        user.ReceiveBlogEmails.Should().BeTrue();
        user.ReceiveBroadcastEmails.Should().BeFalse();
        user.ReceiveGroupEmailsGlobal.Should().BeFalse();
    }

    [Fact]
    public async Task UpdatePersonalInfoAsync_returns_failure_when_user_does_not_exist()
    {
        // Wire FindByIdAsync to return null regardless of id — proves the
        // service refuses to fall through to "anonymous update" if the
        // caller-id claim is somehow stale.
        var store = new Mock<IUserStore<ApplicationUser>>();
        var um = new Mock<UserManager<ApplicationUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        um.Setup(x => x.FindByIdAsync(It.IsAny<string>()))
            .ReturnsAsync((ApplicationUser?)null);
        var sut = new ProfileService(um.Object, Mock.Of<IAuditLogger>());

        var result = await sut.UpdatePersonalInfoAsync(Guid.NewGuid(), new UpdatePersonalInfoRequest(
            null, null, null, null, null, null, null, null, null, null, null));

        result.Succeeded.Should().BeFalse();
        result.Errors[0].Should().Contain("not found");
    }
}
