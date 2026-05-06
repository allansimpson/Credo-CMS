namespace CredoCms.Domain.Settings;

/// <summary>Phase 6 — analytics provider selection. <c>None</c> means no
/// tracking and no consent banner. <c>Ga4</c> enables Google Analytics 4
/// with a cookie consent prompt; gtag only loads after the visitor
/// accepts.</summary>
public enum AnalyticsProvider
{
    None = 0,
    Ga4 = 1,
}

/// <summary>Cookie consent banner placement.</summary>
public enum ConsentBannerPosition
{
    BottomRight = 0,
    BottomFull = 1,
}
