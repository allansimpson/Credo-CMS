using FluentValidation;

namespace CredoCms.Application.Profile;

public sealed class UpdatePersonalInfoRequestValidator : AbstractValidator<UpdatePersonalInfoRequest>
{
    public UpdatePersonalInfoRequestValidator()
    {
        // Phone is optional; if present, ASP.NET Identity's column accepts up
        // to 50 chars (UNICODE) — we cap at 50 to match the EF column.
        RuleFor(x => x.PhoneNumber).MaximumLength(50).When(x => x.PhoneNumber is not null);
        RuleFor(x => x.AddressLine1).MaximumLength(200);
        RuleFor(x => x.AddressLine2).MaximumLength(200);
        RuleFor(x => x.City).MaximumLength(100);
        RuleFor(x => x.StateOrRegion).MaximumLength(100);
        RuleFor(x => x.PostalCode).MaximumLength(20);
        RuleFor(x => x.Country).MaximumLength(100);
        RuleFor(x => x.PhotoBlobUrl).MaximumLength(2000);
        RuleFor(x => x.PhotoWebpBlobUrl).MaximumLength(2000);
        RuleFor(x => x.PhotoAltText).MaximumLength(500);

        // Service-layer enforces "alt text required when photo present" too,
        // but failing fast at validation gives a cleaner 400 response shape.
        RuleFor(x => x.PhotoAltText)
            .NotEmpty()
            .When(x => !string.IsNullOrWhiteSpace(x.PhotoBlobUrl))
            .WithMessage("Alt text is required when a photo is set.");
    }
}

public sealed class UpdateDirectoryRequestValidator : AbstractValidator<UpdateDirectoryRequest>
{
    // No string fields — booleans only. Validator exists for symmetry and as
    // an attachment point for future cross-field rules.
}

public sealed class UpdateNotificationsRequestValidator : AbstractValidator<UpdateNotificationsRequest>
{
    // Booleans only; placeholder for future symmetry.
}
