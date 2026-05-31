namespace CredoCms.Infrastructure.Configuration;

/// <summary>
/// Public-site URL configuration used to compose absolute links in email bodies
/// (invitations, resets). Set via <c>PublicSite:BaseUrl</c> in configuration.
/// </summary>
public sealed class PublicSiteOptions
{
    public const string SectionName = "PublicSite";

    /// <summary>Absolute base URL of the SPA, e.g. https://example.org.
    /// Dev default matches the Vite server in app/vite.config.ts.</summary>
    public string BaseUrl { get; set; } = "http://localhost:5173";
}
