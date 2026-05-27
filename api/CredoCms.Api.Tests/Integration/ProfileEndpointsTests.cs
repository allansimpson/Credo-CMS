using System.Net.Http.Json;
using CredoCms.Application.Profile;
using CredoCms.Application.UserManagement;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CredoCms.Api.Tests.Integration;

/// <summary>
/// Verifies the [Authorize] gate on the four profile endpoints (Q2.5). These
/// tests don't try to authenticate — the goal is to prove that anonymous
/// callers cannot reach any profile mutator. Authenticated cross-user
/// scenarios are covered structurally by ProfileService unit tests, since
/// the service has no API surface that takes a separate target-user id.
/// </summary>
public sealed class ProfileEndpointsTests : IClassFixture<CredoCmsWebAppFactory>
{
    private static readonly WebApplicationFactoryClientOptions NoRedirect = new()
    {
        AllowAutoRedirect = false,
    };

    private readonly CredoCmsWebAppFactory _factory;

    public ProfileEndpointsTests(CredoCmsWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task Get_profile_returns_401_for_anonymous()
    {
        var client = _factory.CreateClient(NoRedirect);
        var response = await client.GetAsync(new Uri("/api/profile", UriKind.Relative));
        ((int)response.StatusCode).Should().Be(401);
    }

    [Fact]
    public async Task Put_personal_returns_401_for_anonymous()
    {
        var client = _factory.CreateClient(NoRedirect);
        var body = new UpdatePersonalInfoRequest(
            null, null, null, null, null, null, null, null, null, null, null);
        var response = await client.PutAsJsonAsync(
            new Uri("/api/profile/personal", UriKind.Relative), body);
        ((int)response.StatusCode).Should().Be(401);
    }

    [Fact]
    public async Task Put_directory_returns_401_for_anonymous()
    {
        var client = _factory.CreateClient(NoRedirect);
        var body = new UpdateDirectoryRequest(false, false, false, false, false);
        var response = await client.PutAsJsonAsync(
            new Uri("/api/profile/directory", UriKind.Relative), body);
        ((int)response.StatusCode).Should().Be(401);
    }

    [Fact]
    public async Task Put_notifications_returns_401_for_anonymous()
    {
        var client = _factory.CreateClient(NoRedirect);
        var body = new UpdateNotificationsRequest(true, false, true, true);
        var response = await client.PutAsJsonAsync(
            new Uri("/api/profile/notifications", UriKind.Relative), body);
        ((int)response.StatusCode).Should().Be(401);
    }

    [Fact]
    public async Task Admin_profile_fields_endpoint_returns_401_for_anonymous()
    {
        var client = _factory.CreateClient(NoRedirect);
        var body = new UpdateUserProfileFieldsRequest(
            null, null, null, null, null, null, null, null, null, null, null,
            false, false, false, false, false, true, false, true, true);
        var response = await client.PutAsJsonAsync(
            new Uri($"/api/admin/users/{Guid.NewGuid()}/profile-fields", UriKind.Relative),
            body);
        ((int)response.StatusCode).Should().Be(401);
    }

    [Fact]
    public async Task Admin_reset_notifications_returns_401_for_anonymous()
    {
        var client = _factory.CreateClient(NoRedirect);
        var response = await client.PostAsync(
            new Uri($"/api/admin/users/{Guid.NewGuid()}/reset-notifications", UriKind.Relative),
            content: null);
        ((int)response.StatusCode).Should().Be(401);
    }
}
