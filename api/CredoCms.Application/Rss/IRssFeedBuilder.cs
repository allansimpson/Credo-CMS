namespace CredoCms.Application.Rss;

/// <summary>
/// Builds an RSS 2.0 XML document for a content type. Items provided by the
/// caller (already filtered to public-only). Atom self-link namespace
/// included so feed readers can self-discover.
/// </summary>
public interface IRssFeedBuilder
{
    /// <summary>Builds an RSS 2.0 XML document. Returned as a UTF-8 byte
    /// buffer so the controller can stream directly with the right
    /// Content-Length.</summary>
    byte[] Build(RssChannelInfo channel, IReadOnlyList<RssItem> items);
}

public sealed record RssChannelInfo(
    string Title,
    string Link,
    string Description,
    string Language,
    string SelfLink);

public sealed record RssItem(
    string Title,
    string Link,
    string Description,
    string Author,
    string Category,
    DateTimeOffset PubDate,
    string PermalinkGuid,
    string? EnclosureUrl = null,
    string? EnclosureType = null,
    long? EnclosureLength = null);
