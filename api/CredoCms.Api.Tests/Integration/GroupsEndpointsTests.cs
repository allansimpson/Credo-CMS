using System.Net.Http.Json;
using CredoCms.Application.Groups;
using CredoCms.Domain.Groups;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CredoCms.Api.Tests.Integration;

/// <summary>
/// Authorization-gate verification for the groups endpoints. Permission rules
/// (admin-only on create / promote, leader-or-admin on approve, etc.) live in
/// GroupService and are covered by GroupServicePermissionTests in the
/// Application test project.
/// </summary>
public sealed class GroupsEndpointsTests : IClassFixture<CredoCmsWebAppFactory>
{
    private static readonly WebApplicationFactoryClientOptions NoRedirect = new()
    {
        AllowAutoRedirect = false,
    };

    private readonly CredoCmsWebAppFactory _factory;
    public GroupsEndpointsTests(CredoCmsWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task Public_groups_list_is_anonymous_and_returns_200()
    {
        var client = _factory.CreateClient(NoRedirect);
        var response = await client.GetAsync(new Uri("/api/public/groups", UriKind.Relative));
        ((int)response.StatusCode).Should().Be(200);
    }

    [Fact]
    public async Task Public_group_detail_returns_404_for_unknown_slug()
    {
        var client = _factory.CreateClient(NoRedirect);
        var response = await client.GetAsync(new Uri("/api/public/groups/no-such-group", UriKind.Relative));
        ((int)response.StatusCode).Should().Be(404);
    }

    [Fact]
    public async Task Public_request_join_returns_401_for_anonymous()
    {
        var client = _factory.CreateClient(NoRedirect);
        var body = new JoinRequestRequest("hi");
        var response = await client.PostAsJsonAsync(
            new Uri("/api/public/groups/youth/request-join", UriKind.Relative), body);
        ((int)response.StatusCode).Should().Be(401);
    }

    [Fact]
    public async Task Profile_groups_returns_401_for_anonymous()
    {
        var client = _factory.CreateClient(NoRedirect);
        var response = await client.GetAsync(new Uri("/api/profile/groups", UriKind.Relative));
        ((int)response.StatusCode).Should().Be(401);
    }

    [Fact]
    public async Task Profile_groups_leave_returns_401_for_anonymous()
    {
        var client = _factory.CreateClient(NoRedirect);
        var response = await client.PostAsync(
            new Uri($"/api/profile/groups/leave/{Guid.NewGuid()}", UriKind.Relative),
            content: null);
        ((int)response.StatusCode).Should().Be(401);
    }

    [Fact]
    public async Task Admin_groups_list_returns_401_for_anonymous()
    {
        var client = _factory.CreateClient(NoRedirect);
        var response = await client.GetAsync(new Uri("/api/admin/groups", UriKind.Relative));
        ((int)response.StatusCode).Should().Be(401);
    }

    [Fact]
    public async Task Admin_groups_create_returns_401_for_anonymous()
    {
        var client = _factory.CreateClient(NoRedirect);
        var body = new CreateGroupRequest("youth", "Youth", null, null, null, null, null, null,
            GroupVisibility.Public, GroupJoinability.Open,
            MessageOnJoinRequest.Optional, RosterVisibility.LeadersOnly, true);
        var response = await client.PostAsJsonAsync(
            new Uri("/api/admin/groups", UriKind.Relative), body);
        ((int)response.StatusCode).Should().Be(401);
    }
}
