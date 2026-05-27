namespace CredoCms.Infrastructure.Configuration;

/// <summary>
/// Public-site URL configuration used to compose absolute links in email bodies
/// (invitations, resets). Set via <c>PublicSite:BaseUrl</c> in configuration.
/// </summary>
public sealed class PublicSiteOptions
{
    public const string SectionName = "PublicSite";

    /// <summary>Absolute base URL of the SPA, e.g. https://example.org.</summary>
    public string BaseUrl { get; set; } = "https://localhost:5001";
}
