namespace CredoCms.Infrastructure.Configuration;

public sealed class CookieAuthOptions
{
    public const string SectionName = "Authentication:Cookie";

    public string Name { get; set; } = ".CredoCms.Auth";

    public string? Domain { get; set; }
}
