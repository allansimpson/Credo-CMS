using System.Net.Http.Json;
using CredoCms.Application.Prayer;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CredoCms.Api.Tests.Integration;

public sealed class PrayerRequestEndpointsTests : IClassFixture<CredoCmsWebAppFactory>
{
    private static readonly WebApplicationFactoryClientOptions NoRedirect = new()
    {
        AllowAutoRedirect = false,
    };

    private readonly CredoCmsWebAppFactory _factory;
    public PrayerRequestEndpointsTests(CredoCmsWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task Member_list_returns_401_for_anonymous()
    {
        var client = _factory.CreateClient(NoRedirect);
        var response = await client.GetAsync(new Uri("/api/prayer-requests", UriKind.Relative));
        ((int)response.StatusCode).Should().Be(401);
    }

    [Fact]
    public async Task Member_submit_returns_401_for_anonymous()
    {
        var client = _factory.CreateClient(NoRedirect);
        var body = new SubmitPrayerRequestRequest("Need prayer", "{}", false);
        var response = await client.PostAsJsonAsync(
            new Uri("/api/prayer-requests", UriKind.Relative), body);
        ((int)response.StatusCode).Should().Be(401);
    }

    [Fact]
    public async Task Mark_prayed_returns_401_for_anonymous()
    {
        var client = _factory.CreateClient(NoRedirect);
        var response = await client.PostAsync(
            new Uri($"/api/prayer-requests/{Guid.NewGuid()}/prayed", UriKind.Relative),
            content: null);
        ((int)response.StatusCode).Should().Be(401);
    }

    [Fact]
    public async Task Admin_list_returns_401_for_anonymous()
    {
        var client = _factory.CreateClient(NoRedirect);
        var response = await client.GetAsync(new Uri("/api/admin/prayer-requests", UriKind.Relative));
        ((int)response.StatusCode).Should().Be(401);
    }

    [Fact]
    public async Task Admin_status_change_returns_401_for_anonymous()
    {
        var client = _factory.CreateClient(NoRedirect);
        var response = await client.PutAsJsonAsync(
            new Uri($"/api/admin/prayer-requests/{Guid.NewGuid()}/status", UriKind.Relative),
            new ChangePrayerStatusRequest(CredoCms.Domain.Prayer.PrayerRequestStatus.Answered));
        ((int)response.StatusCode).Should().Be(401);
    }
}
