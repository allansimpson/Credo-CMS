using FluentValidation;

namespace CredoCms.Application.Prayer;

public sealed class SubmitPrayerRequestRequestValidator : AbstractValidator<SubmitPrayerRequestRequest>
{
    public SubmitPrayerRequestRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.BodyJson).NotEmpty();
    }
}

public sealed class EditPrayerRequestRequestValidator : AbstractValidator<EditPrayerRequestRequest>
{
    public EditPrayerRequestRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.BodyJson).NotEmpty();
    }
}

public sealed class AddPrayerUpdateRequestValidator : AbstractValidator<AddPrayerUpdateRequest>
{
    public AddPrayerUpdateRequestValidator()
    {
        RuleFor(x => x.BodyJson).NotEmpty();
    }
}
