using SendGrid;

namespace CredoCms.Infrastructure.Email;

/// <summary>
/// Factory seam for <see cref="ISendGridClient"/>. The default impl wraps
/// <c>new SendGridClient(apiKey)</c>; tests inject a stub returning a
/// mocked client. Built fresh per send so that a SiteSettings change
/// (rotated API key) takes effect on the next request without restart.
/// </summary>
public interface ISendGridClientFactory
{
    ISendGridClient Create(string apiKey);
}

internal sealed class SendGridClientFactory : ISendGridClientFactory
{
    public ISendGridClient Create(string apiKey) => new SendGridClient(apiKey);
}
