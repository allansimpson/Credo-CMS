namespace CredoCms.Infrastructure.Configuration;

/// <summary>
/// Default-administrator-seed configuration. Read from the <c>Identity</c>
/// section. Both values must be set; in production they should be rotated
/// immediately after first sign-in.
/// </summary>
public sealed class IdentitySeedOptions
{
    public const string SectionName = "Identity";

    public string DefaultAdminEmail { get; set; } = "admin@example.com";

    public string DefaultAdminPassword { get; set; } = "Ch@ngeMeOnFirstLogin!";
}
