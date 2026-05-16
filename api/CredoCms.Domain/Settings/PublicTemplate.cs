namespace CredoCms.Domain.Settings;

/// <summary>
/// Public-facing site template. Picks the visual treatment for every
/// public page (Hero, Sermons, News, etc.) without changing content
/// shape — both templates carry identical content blocks; only the
/// visual rendering differs. Switchable per tenant via the existing
/// Site Settings → Branding tab.
/// </summary>
public enum PublicTemplate
{
    /// <summary>Default. Extends the admin Editorial language onto the
    /// public site — cream + ink + warm accent, dark insets for
    /// callouts. Feels familiar to staff who use the admin daily.</summary>
    Editorial = 0,

    /// <summary>Pared-back, contemporary, generous whitespace. Sage
    /// accent, almost-monochrome neutrals, minimal chrome. Suits
    /// congregations who want a more modern feel.</summary>
    Quiet = 1,
}
