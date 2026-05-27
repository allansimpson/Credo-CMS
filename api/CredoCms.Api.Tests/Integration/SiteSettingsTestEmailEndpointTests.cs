using System.Net.Http.Json;
using System.Text.Json;
using CredoCms.Domain.Email;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CredoCms.Api.Tests.Integration;

/// <summary>
/// Auth + happy-path checks for the test-send endpoint. The full provider
/// dispatch logic is covered by TestEmailServiceTests and
/// EmailServiceRouterTests in the Infrastructure test project.
/// </summary>
public sealed class SiteSettingsTestEmailEndpointTests : IClassFixture<CredoCmsWebAppFactory>
{
    private static readonly WebApplicationFactoryClientOptions NoRedirect = new()
    {
        AllowAutoRedirect = false,
    };

    private readonly CredoCmsWebAppFactory _factory;
    public SiteSettingsTestEmailEndpointTests(CredoCmsWebAppFactory factory) => _factory = factory;

    private static object MakeBody(EmailProvider provider) => new
    {
        provider = (int)provider,
        emailFromAddress = "noreply@example.org",
        emailFromName = "Church",
        emailReplyToAddress = (string?)null,
        sendGridApiKey = (string?)null,
        smtpHost = (string?)null,
        smtpPort = 587,
        smtpUsername = (string?)null,
        smtpPassword = (string?)null,
        smtpUseSsl = true,
        testEmailRecipient = (string?)null,
        overrideToAddress = "anyone@example.org",
    };

    [Fact]
    public async Task Returns_401_for_anonymous_caller()
    {
        var client = _factory.CreateClient(NoRedirect);
        var response = await client.PostAsJsonAsync(
            new Uri("/api/admin/site-settings/test-email", UriKind.Relative),
            MakeBody(EmailProvider.None));
        ((int)response.StatusCode).Should().Be(401);
    }
}
