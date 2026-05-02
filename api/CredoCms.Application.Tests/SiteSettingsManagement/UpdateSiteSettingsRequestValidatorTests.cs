using CredoCms.Application.SiteSettingsManagement;
using FluentValidation.TestHelper;

namespace CredoCms.Application.Tests.SiteSettingsManagement;

public sealed class UpdateSiteSettingsRequestValidatorTests
{
    private readonly UpdateSiteSettingsRequestValidator _v = new();

    private static UpdateSiteSettingsRequest Valid() => new(
        ChurchName: "Hope Community",
        Tagline: "Welcome",
        LogoUrl: null,
        PrimaryColor: "#1e3a8a",
        AccentColor: "#f59e0b",
        ContactEmail: "info@example.org",
        ContactPhone: null,
        ContactAddress: null,
        FacebookUrl: null,
        InstagramUrl: null,
        YouTubeUrl: null,
        XUrl: null,
        TikTokUrl: null,
        OtherSocialLabel: null,
        OtherSocialUrl: null,
        FooterText: null,
        DefaultVersionRetentionCount: 20,
        RowVersion: "AAAAAAAAB9E=");

    [Fact]
    public void Valid_request_passes()
    {
        _v.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ChurchName_is_required()
    {
        var result = _v.TestValidate(Valid() with { ChurchName = "" });
        result.ShouldHaveValidationErrorFor(x => x.ChurchName);
    }

    [Theory]
    [InlineData("not-hex")]
    [InlineData("#zzz")]
    [InlineData("#12345")]
    public void Bad_hex_color_fails(string bad)
    {
        var result = _v.TestValidate(Valid() with { PrimaryColor = bad });
        result.ShouldHaveValidationErrorFor(x => x.PrimaryColor);
    }

    [Theory]
    [InlineData(4)]
    [InlineData(0)]
    [InlineData(51)]
    public void Out_of_range_retention_fails(int n)
    {
        var result = _v.TestValidate(Valid() with { DefaultVersionRetentionCount = n });
        result.ShouldHaveValidationErrorFor(x => x.DefaultVersionRetentionCount);
    }

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("ftp://x")]
    public void Bad_social_url_fails(string bad)
    {
        var result = _v.TestValidate(Valid() with { FacebookUrl = bad });
        result.ShouldHaveValidationErrorFor(x => x.FacebookUrl);
    }
}
