using System.Text.Json;
using FluentValidation;

namespace CredoCms.Application.SiteSettingsManagement;

public sealed class UpdateSiteSettingsRequestValidator : AbstractValidator<UpdateSiteSettingsRequest>
{
    private const string HexColorPattern = @"^#([0-9a-fA-F]{3}|[0-9a-fA-F]{6}|[0-9a-fA-F]{8})$";
    private const long MinDocumentBytes = 1L * 1024 * 1024;          // 1 MB
    private const long MaxDocumentBytesCeiling = 200L * 1024 * 1024; // 200 MB
    private const long MinImageBytes = 1L * 1024 * 1024;             // 1 MB
    private const long MaxImageBytesCeiling = 50L * 1024 * 1024;     // 50 MB

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

        RuleFor(x => x.FacebookUrl).MaximumLength(500).Must(BeValidOptionalAbsoluteUrl);
        RuleFor(x => x.InstagramUrl).MaximumLength(500).Must(BeValidOptionalAbsoluteUrl);
        RuleFor(x => x.YouTubeUrl).MaximumLength(500).Must(BeValidOptionalAbsoluteUrl);
        RuleFor(x => x.XUrl).MaximumLength(500).Must(BeValidOptionalAbsoluteUrl);
        RuleFor(x => x.TikTokUrl).MaximumLength(500).Must(BeValidOptionalAbsoluteUrl);
        RuleFor(x => x.OtherSocialUrl).MaximumLength(500).Must(BeValidOptionalAbsoluteUrl);
        RuleFor(x => x.OtherSocialLabel).MaximumLength(50);

        RuleFor(x => x.FooterText).MaximumLength(500);

        RuleFor(x => x.DefaultVersionRetentionCount).InclusiveBetween(5, 50);

        RuleFor(x => x.LeadersPageLabel).NotEmpty().MaximumLength(100);

        RuleFor(x => x.LeaderCategoriesJson)
            .NotEmpty()
            .Must(BeNonEmptyStringArrayJson)
            .WithMessage("Leader categories must be a non-empty JSON array of strings.");

        RuleFor(x => x.DocumentCategoriesJson)
            .NotEmpty()
            .Must(BeNonEmptyStringArrayJson)
            .WithMessage("Document categories must be a non-empty JSON array of strings.");

        RuleFor(x => x.SermonContextsJson)
            .NotEmpty()
            .Must(BeNonEmptyStringArrayJson)
            .WithMessage("Sermon contexts must be a non-empty JSON array of strings.");

        RuleFor(x => x.MaxDocumentSizeBytes).InclusiveBetween(MinDocumentBytes, MaxDocumentBytesCeiling);
        RuleFor(x => x.MaxImageSizeBytes).InclusiveBetween(MinImageBytes, MaxImageBytesCeiling);

        RuleFor(x => x.ImageMaxWidth).InclusiveBetween(800, 5000);
        RuleFor(x => x.ImageQuality).InclusiveBetween(60, 95);

        RuleFor(x => x.HomepageHeroCtaLabel).NotEmpty().MaximumLength(100);
        RuleFor(x => x.HomepageHeroCtaLink).NotEmpty().MaximumLength(500);

        RuleFor(x => x.DefaultMetaDescription).MaximumLength(300);

        RuleFor(x => x.EmailFromAddress).NotEmpty().EmailAddress().MaximumLength(200);
        RuleFor(x => x.EmailFromName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.EmailReplyToAddress)
            .EmailAddress()
            .When(x => !string.IsNullOrWhiteSpace(x.EmailReplyToAddress))
            .MaximumLength(200);
        RuleFor(x => x.SendGridApiKey).MaximumLength(200);
        RuleFor(x => x.SendGridWebhookSecret).MaximumLength(200);
        RuleFor(x => x.SmtpHost).MaximumLength(200);
        RuleFor(x => x.SmtpPort).InclusiveBetween(1, 65535);
        RuleFor(x => x.SmtpUsername).MaximumLength(200);
        RuleFor(x => x.SmtpPassword).MaximumLength(500);
        RuleFor(x => x.TestEmailRecipient)
            .EmailAddress()
            .When(x => !string.IsNullOrWhiteSpace(x.TestEmailRecipient))
            .MaximumLength(200);
        RuleFor(x => x.NewsEmailTargetGroupIdsJson).Must(BeJsonArray).WithMessage("News email target group ids must be a JSON array.");
        RuleFor(x => x.BlogEmailTargetGroupIdsJson).Must(BeJsonArray).WithMessage("Blog email target group ids must be a JSON array.");
        RuleFor(x => x.EmailSubjectPrefixNews).NotEmpty().MaximumLength(50);
        RuleFor(x => x.EmailSubjectPrefixBlog).NotEmpty().MaximumLength(50);
        RuleFor(x => x.TwilioAccountSid).MaximumLength(200);
        RuleFor(x => x.TwilioAuthToken).MaximumLength(500);
        RuleFor(x => x.TwilioFromNumber).MaximumLength(50);

        RuleFor(x => x.Ga4MeasurementId)
            .Matches(@"^G-[A-Z0-9]{8,12}$")
            .When(x => !string.IsNullOrWhiteSpace(x.Ga4MeasurementId))
            .WithMessage("GA4 measurement ID must look like 'G-XXXXXXXXXX'.")
            .MaximumLength(50);

        // When the operator picks AnalyticsProvider=Ga4 they must also supply a
        // measurement ID — otherwise the cookie banner renders, the user
        // accepts, and gtag never loads (the loader checks for a non-blank
        // id). A silent "consent given but no tracking" is the worst of both
        // worlds. Reject at save time.
        RuleFor(x => x.Ga4MeasurementId)
            .NotEmpty()
            .When(x => x.AnalyticsProvider == Domain.Settings.AnalyticsProvider.Ga4)
            .WithMessage("GA4 measurement ID is required when AnalyticsProvider is GA4.");

        // -------------------------------------------------------------------

        RuleFor(x => x.RowVersion).NotEmpty();
    }

    private static bool BeJsonArray(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        try
        {
            using var doc = JsonDocument.Parse(value);
            return doc.RootElement.ValueKind == JsonValueKind.Array;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool BeValidOptionalAbsoluteUrl(string? value) =>
        string.IsNullOrWhiteSpace(value) ||
        Uri.TryCreate(value, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

    private static bool BeNonEmptyStringArrayJson(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        try
        {
            using var doc = JsonDocument.Parse(value);
            if (doc.RootElement.ValueKind != JsonValueKind.Array) return false;
            var any = false;
            foreach (var el in doc.RootElement.EnumerateArray())
            {
                if (el.ValueKind != JsonValueKind.String) return false;
                if (string.IsNullOrWhiteSpace(el.GetString())) return false;
                any = true;
            }
            return any;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
