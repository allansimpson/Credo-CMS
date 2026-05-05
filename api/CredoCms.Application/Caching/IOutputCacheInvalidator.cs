namespace CredoCms.Application.Caching;

/// <summary>
/// Wraps <c>IOutputCacheStore</c> from Microsoft.AspNetCore.OutputCaching
/// so the Application layer doesn't have to reference ASP.NET Core
/// directly. Implementations live in Infrastructure.
/// </summary>
public interface IOutputCacheInvalidator
{
    /// <summary>Invalidate all cached responses tagged with the given tag.</summary>
    Task InvalidateAsync(string tag, CancellationToken ct = default);

    /// <summary>Invalidate any of multiple tags in one call.</summary>
    Task InvalidateAsync(IEnumerable<string> tags, CancellationToken ct = default);
}

/// <summary>
/// Tag conventions used by cache attributes and invalidation calls. Keep
/// in sync with the [OutputCache(Tags = ...)] declarations on controllers.
/// </summary>
public static class OutputCacheTags
{
    public const string SiteSettings = "site-settings";
    public const string Pages = "pages";
    public const string News = "news";
    public const string ServiceTimes = "service-times";
    public const string Leaders = "leaders";
    public const string Documents = "documents";
    public const string AnnouncementBanner = "announcement";
    public const string Homepage = "homepage";
    public const string Sitemap = "sitemap";
    public const string Search = "search";
}
