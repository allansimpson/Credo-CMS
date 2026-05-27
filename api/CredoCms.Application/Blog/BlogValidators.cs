using FluentValidation;

namespace CredoCms.Application.Blog;

public sealed class CreateBlogPostRequestValidator : AbstractValidator<CreateBlogPostRequest>
{
    public CreateBlogPostRequestValidator()
    {
        RuleFor(x => x.Slug).NotEmpty().MaximumLength(200).Matches("^[a-z0-9-]+$")
            .WithMessage("Slug must use lowercase letters, digits, and dashes only.");
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.BodyJson).NotEmpty();
        RuleFor(x => x.Excerpt).MaximumLength(500);
        RuleFor(x => x.HeroImageAltText).MaximumLength(500);
        RuleFor(x => x.HeroImageAltText)
            .NotEmpty()
            .When(x => !string.IsNullOrWhiteSpace(x.HeroImageBlobUrl))
            .WithMessage("Alt text is required when a hero image is set.");
        RuleFor(x => x.Category).NotEmpty().MaximumLength(100);
        RuleFor(x => x.MetaDescription).MaximumLength(300);
    }
}

public sealed class UpdateBlogPostRequestValidator : AbstractValidator<UpdateBlogPostRequest>
{
    public UpdateBlogPostRequestValidator()
    {
        RuleFor(x => x.Slug).NotEmpty().MaximumLength(200).Matches("^[a-z0-9-]+$");
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.BodyJson).NotEmpty();
        RuleFor(x => x.Excerpt).MaximumLength(500);
        RuleFor(x => x.HeroImageAltText).MaximumLength(500);
        RuleFor(x => x.HeroImageAltText)
            .NotEmpty()
            .When(x => !string.IsNullOrWhiteSpace(x.HeroImageBlobUrl))
            .WithMessage("Alt text is required when a hero image is set.");
        RuleFor(x => x.Category).NotEmpty().MaximumLength(100);
        RuleFor(x => x.MetaDescription).MaximumLength(300);
    }
}
