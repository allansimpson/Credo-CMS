using System.Net.Http.Json;
using CredoCms.Application.Blog;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CredoCms.Api.Tests.Integration;

public sealed class BlogEndpointsTests : IClassFixture<CredoCmsWebAppFactory>
{
    private static readonly WebApplicationFactoryClientOptions NoRedirect = new()
    {
        AllowAutoRedirect = false,
    };

    private readonly CredoCmsWebAppFactory _factory;
    public BlogEndpointsTests(CredoCmsWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task Public_blog_list_is_anonymous_and_returns_200()
    {
        var client = _factory.CreateClient(NoRedirect);
        var response = await client.GetAsync(new Uri("/api/public/blog", UriKind.Relative));
        ((int)response.StatusCode).Should().Be(200);
    }

    [Fact]
    public async Task Public_blog_detail_returns_404_for_unknown_slug()
    {
        var client = _factory.CreateClient(NoRedirect);
        var response = await client.GetAsync(new Uri("/api/public/blog/no-such-post", UriKind.Relative));
        ((int)response.StatusCode).Should().Be(404);
    }

    [Fact]
    public async Task Admin_list_returns_401_for_anonymous()
    {
        var client = _factory.CreateClient(NoRedirect);
        var response = await client.GetAsync(new Uri("/api/admin/blog", UriKind.Relative));
        ((int)response.StatusCode).Should().Be(401);
    }

    [Fact]
    public async Task Admin_create_returns_401_for_anonymous()
    {
        var client = _factory.CreateClient(NoRedirect);
        var body = new CreateBlogPostRequest(
            "post", "Title", """{"type":"doc","content":[]}""",
            null, null, null, null, "Devotional",
            null, true, false, false, null, null, null, null);
        var response = await client.PostAsJsonAsync(
            new Uri("/api/admin/blog", UriKind.Relative), body);
        ((int)response.StatusCode).Should().Be(401);
    }
}
