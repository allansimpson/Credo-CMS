using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CredoCms.Api.Tests.Integration;

/// <summary>
/// Auth checks for /api/webhooks/sendgrid. Signature verification + event
/// processing logic are covered by SendGridWebhookEventProcessorTests in
/// the Application test project against a Moq'd verifier.
/// </summary>
public sealed class SendGridWebhookTests : IClassFixture<CredoCmsWebAppFactory>
{
    private static readonly WebApplicationFactoryClientOptions NoRedirect = new()
    {
        AllowAutoRedirect = false,
    };

    private readonly CredoCmsWebAppFactory _factory;
    public SendGridWebhookTests(CredoCmsWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task Returns_401_when_signature_headers_missing()
    {
        var client = _factory.CreateClient(NoRedirect);
        var content = new StringContent("[]", Encoding.UTF8, "application/json");
        var response = await client.PostAsync(new Uri("/api/webhooks/sendgrid", UriKind.Relative), content);
        ((int)response.StatusCode).Should().Be(401);
    }

    [Fact]
    public async Task Returns_401_when_signature_invalid()
    {
        var client = _factory.CreateClient(NoRedirect);
        var content = new StringContent("[]", Encoding.UTF8, "application/json");
        // Garbage signature + valid-shape timestamp.
        client.DefaultRequestHeaders.Add("X-Twilio-Email-Event-Webhook-Signature", "garbage");
        client.DefaultRequestHeaders.Add("X-Twilio-Email-Event-Webhook-Timestamp", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
        var response = await client.PostAsync(new Uri("/api/webhooks/sendgrid", UriKind.Relative), content);
        ((int)response.StatusCode).Should().Be(401);
    }
}
