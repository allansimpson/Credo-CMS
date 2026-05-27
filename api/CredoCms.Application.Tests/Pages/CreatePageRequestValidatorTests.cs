using CredoCms.Application.Pages;
using FluentValidation.TestHelper;

namespace CredoCms.Application.Tests.Pages;

public sealed class CreatePageRequestValidatorTests
{
    private readonly CreatePageRequestValidator _v = new();

    private static CreatePageRequest Valid() => new(
        Slug: "plan-your-visit",
        Title: "Plan Your Visit",
        BodyJson: """{"type":"doc","content":[]}""",
        Excerpt: null,
        HeroImageUrl: null,
        HeroImageWebpUrl: null,
        HeroImageAlt: null,
        MetaDescription: null,
        IsPublished: true,
        IsMembersOnly: false);

    [Fact]
    public void Valid_request_passes()
        => _v.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();

    [Theory]
    [InlineData("")]
    [InlineData("UPPERCASE")]
    [InlineData("under_score")]
    [InlineData("trailing-")]
    [InlineData("-leading")]
    [InlineData("double--dash")]
    [InlineData("has space")]
    public void Bad_slug_fails(string bad)
    {
        var result = _v.TestValidate(Valid() with { Slug = bad });
        result.ShouldHaveValidationErrorFor(x => x.Slug);
    }

    [Fact]
    public void Empty_title_fails()
    {
        _v.TestValidate(Valid() with { Title = "" })
            .ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void Empty_body_fails()
    {
        _v.TestValidate(Valid() with { BodyJson = "" })
            .ShouldHaveValidationErrorFor(x => x.BodyJson);
    }

    [Fact]
    public void Long_meta_description_fails()
    {
        _v.TestValidate(Valid() with { MetaDescription = new string('x', 301) })
            .ShouldHaveValidationErrorFor(x => x.MetaDescription);
    }
}
