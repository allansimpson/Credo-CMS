using CredoCms.Application.Pages;
using FluentValidation;

namespace CredoCms.Application.News;

public sealed class CreateNewsItemRequestValidator : AbstractValidator<CreateNewsItemRequest>
{
    public CreateNewsItemRequestValidator()
    {
        RuleFor(x => x.Slug).ValidSlug();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.BodyJson).NotEmpty();
        RuleFor(x => x.Excerpt).MaximumLength(500);
        RuleFor(x => x.HeroImageUrl).MaximumLength(2000);
        RuleFor(x => x.HeroImageWebpUrl).MaximumLength(2000);
        RuleFor(x => x.HeroImageAlt).MaximumLength(300);
        RuleFor(x => x.MetaDescription).MaximumLength(300);
    }
}

public sealed class UpdateNewsItemRequestValidator : AbstractValidator<UpdateNewsItemRequest>
{
    public UpdateNewsItemRequestValidator()
    {
        RuleFor(x => x.Slug).ValidSlug();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.BodyJson).NotEmpty();
        RuleFor(x => x.Excerpt).MaximumLength(500);
        RuleFor(x => x.HeroImageUrl).MaximumLength(2000);
        RuleFor(x => x.HeroImageWebpUrl).MaximumLength(2000);
        RuleFor(x => x.HeroImageAlt).MaximumLength(300);
        RuleFor(x => x.MetaDescription).MaximumLength(300);
    }
}
