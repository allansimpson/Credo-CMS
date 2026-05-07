using System.Globalization;
using System.Text;
using System.Xml;
using CredoCms.Application.Rss;

namespace CredoCms.Infrastructure.Rss;

public sealed class RssFeedBuilder : IRssFeedBuilder
{
    public byte[] Build(RssChannelInfo channel, IReadOnlyList<RssItem> items)
    {
        ArgumentNullException.ThrowIfNull(channel);
        ArgumentNullException.ThrowIfNull(items);

        var settings = new XmlWriterSettings
        {
            Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            Indent = false,
            OmitXmlDeclaration = false,
            Async = false,
        };
        using var ms = new MemoryStream();
        using (var writer = XmlWriter.Create(ms, settings))
        {
            writer.WriteStartDocument();
            writer.WriteStartElement("rss");
            writer.WriteAttributeString("version", "2.0");
            writer.WriteAttributeString("xmlns", "atom", null, "http://www.w3.org/2005/Atom");

            writer.WriteStartElement("channel");
            writer.WriteElementString("title", channel.Title);
            writer.WriteElementString("link", channel.Link);
            writer.WriteElementString("description", channel.Description);
            writer.WriteElementString("language", channel.Language);

            // Atom self-link — required by RFC 5005 / aggregator best
            // practice so feed readers can detect the canonical URL.
            writer.WriteStartElement("atom", "link", "http://www.w3.org/2005/Atom");
            writer.WriteAttributeString("href", channel.SelfLink);
            writer.WriteAttributeString("rel", "self");
            writer.WriteAttributeString("type", "application/rss+xml");
            writer.WriteEndElement();

            if (items.Count > 0)
            {
                var latest = items.Max(i => i.PubDate);
                writer.WriteElementString("lastBuildDate", FormatRfc822(latest));
            }

            foreach (var item in items)
            {
                writer.WriteStartElement("item");
                writer.WriteElementString("title", item.Title);
                writer.WriteElementString("link", item.Link);
                // Description CDATA-wrapped so HTML excerpts don't need
                // entity-encoding.
                writer.WriteStartElement("description");
                writer.WriteCData(item.Description ?? string.Empty);
                writer.WriteEndElement();
                if (!string.IsNullOrWhiteSpace(item.Author))
                    writer.WriteElementString("author", item.Author);
                if (!string.IsNullOrWhiteSpace(item.Category))
                    writer.WriteElementString("category", item.Category);
                writer.WriteElementString("pubDate", FormatRfc822(item.PubDate));

                writer.WriteStartElement("guid");
                writer.WriteAttributeString("isPermaLink", "true");
                writer.WriteString(item.PermalinkGuid);
                writer.WriteEndElement();

                if (!string.IsNullOrWhiteSpace(item.EnclosureUrl))
                {
                    writer.WriteStartElement("enclosure");
                    writer.WriteAttributeString("url", item.EnclosureUrl);
                    writer.WriteAttributeString("type", item.EnclosureType ?? "image/jpeg");
                    // SendGrid-style RSS readers tolerate length=0 when
                    // unknown; some others ignore the field. Always emit
                    // for spec compliance.
                    writer.WriteAttributeString("length",
                        (item.EnclosureLength ?? 0).ToString(CultureInfo.InvariantCulture));
                    writer.WriteEndElement();
                }

                writer.WriteEndElement(); // </item>
            }

            writer.WriteEndElement(); // </channel>
            writer.WriteEndElement(); // </rss>
            writer.WriteEndDocument();
        }
        return ms.ToArray();
    }

    /// <summary>RFC 822 / RSS 2.0 date format.</summary>
    private static string FormatRfc822(DateTimeOffset dt) =>
        dt.ToUniversalTime().ToString("ddd, dd MMM yyyy HH:mm:ss") + " +0000";
}
