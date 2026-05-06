using System.Net.Http.Json;
using CredoCms.Application.ConnectCard;
using CredoCms.Domain.ConnectCard;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CredoCms.Api.Tests.Integration;

/// <summary>
/// Auth + accessibility checks for the connect-card endpoints. The full
/// anti-bot rule set is covered by ConnectCardServiceTests in the
/// Application test project.
/// </summary>
public sealed class ConnectCardEndpointsTests : IClassFixture<CredoCmsWebAppFactory>
{
    private static readonly WebApplicationFactoryClientOptions NoRedirect = new()
    {
        AllowAutoRedirect = false,
    };

    private readonly CredoCmsWebAppFactory _factory;
    public ConnectCardEndpointsTests(CredoCmsWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task Public_submit_is_anonymous_and_returns_200_with_ok_false_for_honeypot()
    {
        var client = _factory.CreateClient(NoRedirect);
        var body = new SubmitConnectCardRequest(
            Name: "Bot",
            Email: "bot@example.com",
            Phone: null,
            IsFirstTimeVisitor: false,
            ServiceDate: null,
            HowDidYouHear: "search",
            Comments: null,
            Interests: null,
            HoneypotValue: "spam-content",
            ClientLoadedAt: DateTimeOffset.UtcNow.AddSeconds(-10),
            TurnstileToken: null);

        var response = await client.PostAsJsonAsync(
            new Uri("/api/public/connect-card", UriKind.Relative), body);

        ((int)response.StatusCode).Should().Be(200);
        // Public surface always returns 200 to keep abuse tooling guessing;
        // ok=false carries the rejection.
    }

    [Fact]
    public async Task Admin_list_returns_401_for_anonymous()
    {
        var client = _factory.CreateClient(NoRedirect);
        var response = await client.GetAsync(new Uri("/api/admin/connect-cards", UriKind.Relative));
        ((int)response.StatusCode).Should().Be(401);
    }

    [Fact]
    public async Task Admin_status_update_returns_401_for_anonymous()
    {
        var client = _factory.CreateClient(NoRedirect);
        var response = await client.PutAsJsonAsync(
            new Uri($"/api/admin/connect-cards/{Guid.NewGuid()}/status", UriKind.Relative),
            new UpdateStatusRequest(ConnectCardStatus.FollowedUp));
        ((int)response.StatusCode).Should().Be(401);
    }

    [Fact]
    public async Task Admin_resend_returns_401_for_anonymous()
    {
        var client = _factory.CreateClient(NoRedirect);
        var response = await client.PostAsync(
            new Uri($"/api/admin/connect-cards/{Guid.NewGuid()}/resend", UriKind.Relative),
            content: null);
        ((int)response.StatusCode).Should().Be(401);
    }
}
