using FluentValidation;

namespace CredoCms.Application.SiteSettingsManagement;

public sealed class UpdateSiteSettingsRequestValidator : AbstractValidator<UpdateSiteSettingsRequest>
{
    private const string HexColorPattern = @"^#([0-9a-fA-F]{3}|[0-9a-fA-F]{6}|[0-9a-fA-F]{8})$";

    public UpdateSiteSettingsRequestValidator()
    {
        RuleFor(x => x.ChurchName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Tagline).MaximumLength(300);
        RuleFor(x => x.LogoUrl).MaximumLength(2000);

        RuleFor(x => x.PrimaryColor)
            .NotEmpty()
            .Matches(HexColorPattern)
            .WithMessage("Primary color must be a hex value like #1e3a8a.");

        RuleFor(x => x.AccentColor)
            .NotEmpty()
            .Matches(HexColorPattern)
            .WithMessage("Accent color must be a hex value like #f59e0b.");

        RuleFor(x => x.ContactEmail)
            .EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.ContactEmail))
            .MaximumLength(254);

        RuleFor(x => x.ContactPhone).MaximumLength(50);
        RuleFor(x => x.ContactAddress).MaximumLength(500);

        foreach (var url in new[] { nameof(UpdateSiteSettingsRequest.FacebookUrl),
                                    nameof(UpdateSiteSettingsRequest.InstagramUrl),
                                    nameof(UpdateSiteSettingsRequest.YouTubeUrl),
                                    nameof(UpdateSiteSettingsRequest.XUrl),
                                    nameof(UpdateSiteSettingsRequest.TikTokUrl),
                                    nameof(UpdateSiteSettingsRequest.OtherSocialUrl) })
        {
            // FluentValidation doesn't support reflection-driven rules elegantly, so
            // each url is also validated below by name. Length cap is centralized here.
        }

        RuleFor(x => x.FacebookUrl).MaximumLength(500).Must(BeValidOptionalAbsoluteUrl);
        RuleFor(x => x.InstagramUrl).MaximumLength(500).Must(BeValidOptionalAbsoluteUrl);
        RuleFor(x => x.YouTubeUrl).MaximumLength(500).Must(BeValidOptionalAbsoluteUrl);
        RuleFor(x => x.XUrl).MaximumLength(500).Must(BeValidOptionalAbsoluteUrl);
        RuleFor(x => x.TikTokUrl).MaximumLength(500).Must(BeValidOptionalAbsoluteUrl);
        RuleFor(x => x.OtherSocialUrl).MaximumLength(500).Must(BeValidOptionalAbsoluteUrl);
        RuleFor(x => x.OtherSocialLabel).MaximumLength(50);

        RuleFor(x => x.FooterText).MaximumLength(500);

        RuleFor(x => x.DefaultVersionRetentionCount).InclusiveBetween(5, 50);

        RuleFor(x => x.RowVersion).NotEmpty();
    }

    private static bool BeValidOptionalAbsoluteUrl(string? value) =>
        string.IsNullOrWhiteSpace(value) ||
        Uri.TryCreate(value, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
