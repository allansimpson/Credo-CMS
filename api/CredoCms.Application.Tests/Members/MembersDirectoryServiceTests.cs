using CredoCms.Application.Common;
using CredoCms.Application.Members;
using Moq;

namespace CredoCms.Application.Tests.Members;

/// <summary>
/// Service-layer unit tests for the privacy filter. The opt-in gate
/// (IsListedInDirectory + IsActive) is enforced at the query layer; these
/// tests focus on what the service itself owns: nulling out fields the
/// member did not opt in to share.
/// </summary>
public sealed class MembersDirectoryServiceTests
{
    private static MemberDirectoryRow MakeRow(
        bool email = false,
        bool phone = false,
        bool address = false,
        bool photo = false) => new(
            UserId: Guid.NewGuid(),
            FirstName: "Alice",
            LastName: "Adams",
            Email: "alice@example.com",
            PhoneNumber: "555-0100",
            AddressLine1: "1 Main",
            AddressLine2: null,
            City: "Springfield",
            StateOrRegion: "OR",
            PostalCode: "97000",
            Country: "USA",
            PhotoBlobUrl: "https://example.com/photo.jpg",
            PhotoWebpBlobUrl: "https://example.com/photo.webp",
            PhotoAltText: "Headshot of Alice",
            PublicAuthorBio: "Bio text",
            ShowEmailInDirectory: email,
            ShowPhoneInDirectory: phone,
            ShowAddressInDirectory: address,
            ShowPhotoInDirectory: photo);

    [Fact]
    public async Task ListAsync_strips_all_optional_fields_when_no_opt_in()
    {
        var queries = new Mock<IMembersDirectoryQueries>();
        queries.Setup(x => x.ListAsync(It.IsAny<MembersDirectoryQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<MemberDirectoryRow>(
                new[] { MakeRow() }, 1, 1, 24));
        var sut = new MembersDirectoryService(queries.Object);

        var result = await sut.ListAsync(new MembersDirectoryQuery());

        var row = result.Items.Single();
        row.Email.Should().BeNull();
        row.PhoneNumber.Should().BeNull();
        row.PhotoBlobUrl.Should().BeNull();
        row.PhotoWebpBlobUrl.Should().BeNull();
        row.PhotoAltText.Should().BeNull();
    }

    [Fact]
    public async Task ListAsync_returns_opted_in_fields_only()
    {
        var queries = new Mock<IMembersDirectoryQueries>();
        queries.Setup(x => x.ListAsync(It.IsAny<MembersDirectoryQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<MemberDirectoryRow>(
                new[] { MakeRow(email: true, photo: true) }, 1, 1, 24));
        var sut = new MembersDirectoryService(queries.Object);

        var result = await sut.ListAsync(new MembersDirectoryQuery());

        var row = result.Items.Single();
        row.Email.Should().Be("alice@example.com");
        row.PhoneNumber.Should().BeNull();
        row.PhotoBlobUrl.Should().Be("https://example.com/photo.jpg");
    }

    [Fact]
    public async Task GetByIdAsync_returns_null_when_query_layer_returns_null()
    {
        var queries = new Mock<IMembersDirectoryQueries>();
        queries.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MemberDirectoryRow?)null);
        var sut = new MembersDirectoryService(queries.Object);

        var result = await sut.GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_strips_address_fields_when_not_opted_in()
    {
        var queries = new Mock<IMembersDirectoryQueries>();
        queries.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeRow(email: true)); // address opt-in is false
        queries.Setup(x => x.ListVisibleGroupsForUserAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<MemberGroupMembershipDto>());
        var sut = new MembersDirectoryService(queries.Object);

        var result = await sut.GetByIdAsync(Guid.NewGuid());

        result.Should().NotBeNull();
        result!.AddressLine1.Should().BeNull();
        result.City.Should().BeNull();
        result.StateOrRegion.Should().BeNull();
        result.Country.Should().BeNull();
        result.Email.Should().Be("alice@example.com");
    }

    [Fact]
    public async Task GetByIdAsync_includes_public_author_bio_unconditionally()
    {
        // PublicAuthorBio is not gated by a per-field toggle — surfaced any
        // time the user is in the directory at all.
        var queries = new Mock<IMembersDirectoryQueries>();
        queries.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeRow());
        queries.Setup(x => x.ListVisibleGroupsForUserAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<MemberGroupMembershipDto>());
        var sut = new MembersDirectoryService(queries.Object);

        var result = await sut.GetByIdAsync(Guid.NewGuid());

        result!.PublicAuthorBio.Should().Be("Bio text");
    }
}
