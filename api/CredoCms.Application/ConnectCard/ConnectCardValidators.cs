using FluentValidation;

namespace CredoCms.Application.ConnectCard;

public sealed class SubmitConnectCardRequestValidator : AbstractValidator<SubmitConnectCardRequest>
{
    public SubmitConnectCardRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).MaximumLength(200).EmailAddress()
            .When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Phone).MaximumLength(50);
        RuleFor(x => x.HowDidYouHear).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Comments).MaximumLength(5000);

        // At least one of email or phone — the church needs a way to reach
        // back. If neither is supplied the form is rejected.
        RuleFor(x => x).Must(r => !string.IsNullOrWhiteSpace(r.Email) || !string.IsNullOrWhiteSpace(r.Phone))
            .WithMessage("Please provide at least an email address or a phone number so we can follow up.");
    }
}
