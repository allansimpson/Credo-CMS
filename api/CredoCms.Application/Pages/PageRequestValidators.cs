using System.Text.RegularExpressions;
using FluentValidation;

namespace CredoCms.Application.Pages;

public static partial class PageValidationRules
{
    /// <summary>Slugs must be lower-case, dash-separated, alphanumeric.</summary>
    [GeneratedRegex(@"^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.CultureInvariant)]
    public static partial Regex SlugRegex();

    public static IRuleBuilderOptions<T, string> ValidSlug<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty()
            .MaximumLength(200)
            .Matches(SlugRegex())
            .WithMessage("Slug must be lower-case letters, digits, and dashes (e.g. 'plan-your-visit').");
}

public sealed class CreatePageRequestValidator : AbstractValidator<CreatePageRequest>
{
    public CreatePageRequestValidator()
    {
        RuleFor(x => x.Slug).ValidSlug();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.BodyJson).NotEmpty();
        RuleFor(x => x.Excerpt).MaximumLength(500);
        RuleFor(x => x.HeroImageUrl).MaximumLength(2000);
        RuleFor(x => x.HeroImageWebpUrl).MaximumLength(2000);
        RuleFor(x => x.HeroImageAlt).MaximumLength(300);
        RuleFor(x => x.MetaDescription).MaximumLength(300);
        RuleFor(x => x.Template).IsInEnum();
    }
}

public sealed class UpdatePageRequestValidator : AbstractValidator<UpdatePageRequest>
{
    public UpdatePageRequestValidator()
    {
        RuleFor(x => x.Slug).ValidSlug();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.BodyJson).NotEmpty();
        RuleFor(x => x.Excerpt).MaximumLength(500);
        RuleFor(x => x.HeroImageUrl).MaximumLength(2000);
        RuleFor(x => x.HeroImageWebpUrl).MaximumLength(2000);
        RuleFor(x => x.HeroImageAlt).MaximumLength(300);
        RuleFor(x => x.MetaDescription).MaximumLength(300);
        RuleFor(x => x.Template).IsInEnum();
    }
}
