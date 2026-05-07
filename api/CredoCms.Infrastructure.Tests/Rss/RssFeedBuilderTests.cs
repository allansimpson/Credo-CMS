using System.Xml.Linq;
using CredoCms.Application.Rss;
using CredoCms.Infrastructure.Rss;

namespace CredoCms.Infrastructure.Tests.Rss;

public sealed class RssFeedBuilderTests
{
    private static RssChannelInfo Channel() => new(
        Title: "Hope Community — Blog",
        Link: "https://example.org/blog",
        Description: "Latest blog posts from Hope Community",
        Language: "en-us",
        SelfLink: "https://example.org/blog/rss.xml");

    private static RssItem Item(string title = "First post", string? enclosure = null) => new(
        Title: title,
        Link: $"https://example.org/blog/{title.ToLowerInvariant().Replace(' ', '-')}",
        Description: "<p>Hello there</p>",
        Author: "Alice Chen",
        Category: "Devotional",
        PubDate: new DateTimeOffset(2026, 5, 1, 12, 0, 0, TimeSpan.Zero),
        PermalinkGuid: $"https://example.org/blog/{title.ToLowerInvariant().Replace(' ', '-')}",
        EnclosureUrl: enclosure,
        EnclosureType: enclosure is not null ? "image/jpeg" : null);

    [Fact]
    public void Builds_well_formed_RSS_2_with_atom_self_link()
    {
        var sut = new RssFeedBuilder();
        var bytes = sut.Build(Channel(), new[] { Item() });

        var doc = XDocument.Parse(System.Text.Encoding.UTF8.GetString(bytes));
        var rss = doc.Root!;
        rss.Name.LocalName.Should().Be("rss");
        rss.Attribute("version")!.Value.Should().Be("2.0");

        XNamespace atom = "http://www.w3.org/2005/Atom";
        var channel = rss.Element("channel")!;
        var atomLink = channel.Element(atom + "link")!;
        atomLink.Attribute("rel")!.Value.Should().Be("self");
        atomLink.Attribute("href")!.Value.Should().Be("https://example.org/blog/rss.xml");
    }

    [Fact]
    public void Item_pubDate_is_RFC_822_format()
    {
        var sut = new RssFeedBuilder();
        var bytes = sut.Build(Channel(), new[] { Item() });
        var doc = XDocument.Parse(System.Text.Encoding.UTF8.GetString(bytes));
        var pubDate = doc.Descendants("pubDate").First().Value;
        // "Fri, 01 May 2026 12:00:00 +0000"
        pubDate.Should().MatchRegex(@"^[A-Z][a-z]{2}, \d{2} [A-Z][a-z]{2} \d{4} \d{2}:\d{2}:\d{2} \+\d{4}$");
    }

    [Fact]
    public void Description_is_CDATA_wrapped()
    {
        var sut = new RssFeedBuilder();
        var bytes = sut.Build(Channel(), new[] { Item() });
        var raw = System.Text.Encoding.UTF8.GetString(bytes);
        raw.Should().Contain("<![CDATA[<p>Hello there</p>]]>");
    }

    [Fact]
    public void Enclosure_emitted_when_url_set()
    {
        var sut = new RssFeedBuilder();
        var bytes = sut.Build(Channel(), new[] { Item(enclosure: "https://example.org/img/hero.jpg") });
        var doc = XDocument.Parse(System.Text.Encoding.UTF8.GetString(bytes));
        var enclosure = doc.Descendants("enclosure").FirstOrDefault();
        enclosure.Should().NotBeNull();
        enclosure!.Attribute("url")!.Value.Should().Be("https://example.org/img/hero.jpg");
        enclosure.Attribute("type")!.Value.Should().Be("image/jpeg");
    }

    [Fact]
    public void Empty_feed_omits_lastBuildDate()
    {
        var sut = new RssFeedBuilder();
        var bytes = sut.Build(Channel(), Array.Empty<RssItem>());
        var doc = XDocument.Parse(System.Text.Encoding.UTF8.GetString(bytes));
        doc.Descendants("lastBuildDate").Should().BeEmpty();
    }

    [Fact]
    public void Guid_is_PermaLink_true()
    {
        var sut = new RssFeedBuilder();
        var bytes = sut.Build(Channel(), new[] { Item() });
        var doc = XDocument.Parse(System.Text.Encoding.UTF8.GetString(bytes));
        var guid = doc.Descendants("guid").First();
        guid.Attribute("isPermaLink")!.Value.Should().Be("true");
    }
}
