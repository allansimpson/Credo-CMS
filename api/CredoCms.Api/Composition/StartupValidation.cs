using CredoCms.Application.Events;

namespace CredoCms.Api.Composition;

internal static class StartupValidation
{
    /// <summary>
    /// In Production, refuse to boot if the token-signing secret is unset or
    /// still equals the default dev value baked into source. Dev / Testing
    /// builds continue silently so contributors can run without configuring
    /// a custom secret.
    /// </summary>
    public static void ValidateProductionConfiguration(this WebApplicationBuilder builder)
    {
        if (!builder.Environment.IsProduction()) return;

        var configuredSecret = builder.Configuration[
            $"{RegistrationTokenSignerOptions.SectionName}:TokenSigningSecret"];
        if (string.IsNullOrWhiteSpace(configuredSecret)
            || configuredSecret == RegistrationTokenSignerOptions.DefaultDevSecret)
        {
            throw new InvalidOperationException(
                $"{RegistrationTokenSignerOptions.SectionName}:TokenSigningSecret "
                + "is unset or still using the default dev value. Production deployments must override this "
                + "via configuration (App Service application setting or Key Vault).");
        }
    }
}
