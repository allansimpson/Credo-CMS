using CredoCms.Application.YouTube;

namespace CredoCms.Application.Tests.YouTube;

public sealed class YouTubeUrlParserTests
{
    [Theory]
    [InlineData("dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://youtube.com/watch?v=dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/watch?feature=youtu.be&v=dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://youtu.be/dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/shorts/dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/embed/dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    public void Parses_known_url_shapes(string input, string expected)
    {
        YouTubeUrlParser.TryParse(input).Should().Be(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("not-a-video-id-or-url")]
    [InlineData("https://example.com/page")]
    public void Returns_null_for_unparseable_input(string? input)
    {
        YouTubeUrlParser.TryParse(input).Should().BeNull();
    }
}
