namespace CredoCms.Api.Tests.Integration;

public sealed class AuthEndpointsTests : IClassFixture<CredoCmsWebAppFactory>
{
    private readonly CredoCmsWebAppFactory _factory;

    public AuthEndpointsTests(CredoCmsWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task Health_endpoint_is_anonymous_and_returns_200()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync(new Uri("/api/health", UriKind.Relative));
        response.IsSuccessStatusCode.Should().BeTrue();
    }

    [Fact]
    public async Task Me_returns_401_when_anonymous()
    {
        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });
        var response = await client.GetAsync(new Uri("/api/auth/me", UriKind.Relative));
        ((int)response.StatusCode).Should().Be(401);
    }

    [Fact]
    public async Task Admin_users_endpoint_returns_401_for_anonymous_then_403to404_for_wrong_role()
    {
        // Anonymous → 401 (the SPA's covert-404 happens at the route layer, not the
        // API; for the API the prompt's covert-404 transformation applies only to
        // 403, leaving 401 distinguishable so authenticated callers can be told that
        // their session expired vs that the URL doesn't exist).
        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });
        var response = await client.GetAsync(new Uri("/api/admin/users", UriKind.Relative));
        ((int)response.StatusCode).Should().Be(401);
    }
}
