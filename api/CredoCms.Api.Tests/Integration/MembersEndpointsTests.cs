using Microsoft.AspNetCore.Mvc.Testing;

namespace CredoCms.Api.Tests.Integration;

/// <summary>
/// Verifies the [Authorize] role gate on the members directory (Q4.5(a)).
/// The data-shape guarantees ((b) non-listed → 404, (c) non-opted-in fields
/// absent) are covered by MembersDirectoryServiceTests in the Application
/// test project, which is the layer where those rules live.
/// </summary>
public sealed class MembersEndpointsTests : IClassFixture<CredoCmsWebAppFactory>
{
    private static readonly WebApplicationFactoryClientOptions NoRedirect = new()
    {
        AllowAutoRedirect = false,
    };

    private readonly CredoCmsWebAppFactory _factory;

    public MembersEndpointsTests(CredoCmsWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task List_members_returns_401_for_anonymous()
    {
        var client = _factory.CreateClient(NoRedirect);
        var response = await client.GetAsync(new Uri("/api/members", UriKind.Relative));
        ((int)response.StatusCode).Should().Be(401);
    }

    [Fact]
    public async Task Get_member_detail_returns_401_for_anonymous()
    {
        var client = _factory.CreateClient(NoRedirect);
        var response = await client.GetAsync(
            new Uri($"/api/members/{Guid.NewGuid()}", UriKind.Relative));
        ((int)response.StatusCode).Should().Be(401);
    }
}
