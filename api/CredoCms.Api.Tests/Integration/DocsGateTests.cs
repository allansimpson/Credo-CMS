using Microsoft.AspNetCore.Mvc.Testing;

namespace CredoCms.Api.Tests.Integration;

/// <summary>
/// Covert routing for the /docs/* docs site. Anonymous and
/// Member visitors should see 404 (covert), never 401 / 403 / a docs
/// 200. Editor / Administrator return 200 (or 404 if the docs haven't
/// been built into wwwroot yet — both are acceptable here since we're
/// only verifying the auth gate, not the content).
/// </summary>
public sealed class DocsGateTests : IClassFixture<CredoCmsWebAppFactory>
{
    private static readonly WebApplicationFactoryClientOptions NoRedirect = new()
    {
        AllowAutoRedirect = false,
    };

    private readonly CredoCmsWebAppFactory _factory;
    public DocsGateTests(CredoCmsWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task Anonymous_request_to_docs_root_is_covert_404()
    {
        var client = _factory.CreateClient(NoRedirect);
        var response = await client.GetAsync(new Uri("/docs", UriKind.Relative));
        ((int)response.StatusCode).Should().Be(404);
    }

    [Fact]
    public async Task Anonymous_request_to_docs_subpath_is_covert_404()
    {
        var client = _factory.CreateClient(NoRedirect);
        var response = await client.GetAsync(new Uri("/docs/getting-started/", UriKind.Relative));
        ((int)response.StatusCode).Should().Be(404);
    }

    [Fact]
    public async Task Path_traversal_returns_404()
    {
        var client = _factory.CreateClient(NoRedirect);
        var response = await client.GetAsync(new Uri("/docs/../appsettings.json", UriKind.Relative));
        ((int)response.StatusCode).Should().Be(404);
    }
}
