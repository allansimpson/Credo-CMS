using CredoCms.Api.Middleware;
using Microsoft.AspNetCore.Http;

namespace CredoCms.Api.Tests.Middleware;

public sealed class ForbiddenToNotFoundMiddlewareTests
{
    [Theory]
    [InlineData("/api/admin/users")]
    [InlineData("/API/Admin/Users")]
    [InlineData("/api/docs")]
    public async Task Forbidden_on_covert_path_becomes_404(string path)
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Path = path;
        ctx.Response.StatusCode = StatusCodes.Status403Forbidden;

        var middleware = new ForbiddenToNotFoundMiddleware(_ => Task.CompletedTask);
        await middleware.InvokeAsync(ctx);

        ctx.Response.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [Theory]
    [InlineData("/api/auth/me")]
    [InlineData("/api/site-settings/public")]
    [InlineData("/")]
    public async Task Forbidden_on_other_path_stays_403(string path)
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Path = path;
        ctx.Response.StatusCode = StatusCodes.Status403Forbidden;

        var middleware = new ForbiddenToNotFoundMiddleware(_ => Task.CompletedTask);
        await middleware.InvokeAsync(ctx);

        ctx.Response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
    }

    [Fact]
    public async Task Non_403_status_passes_through_unchanged()
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Path = "/api/admin/users";
        ctx.Response.StatusCode = StatusCodes.Status200OK;

        var middleware = new ForbiddenToNotFoundMiddleware(_ => Task.CompletedTask);
        await middleware.InvokeAsync(ctx);

        ctx.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
    }
}
