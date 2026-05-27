using FluentValidation;

namespace CredoCms.Application.Classes;

public sealed class CreateClassSlotRequestValidator : AbstractValidator<CreateClassSlotRequest>
{
    public CreateClassSlotRequestValidator()
    {
        RuleFor(x => x.Slug).NotEmpty().MaximumLength(200)
            .Matches("^[a-z0-9-]+$")
            .WithMessage("Slug must use lowercase letters, digits, and dashes only.");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.AudienceAgeGroup).NotEmpty().MaximumLength(100);
        RuleFor(x => x.GeneralMeetingTime).MaximumLength(200);
        RuleFor(x => x.DefaultRoom).MaximumLength(200);
        RuleFor(x => x.ImageAltText).MaximumLength(500);
        RuleFor(x => x.ImageAltText)
            .NotEmpty()
            .When(x => !string.IsNullOrWhiteSpace(x.ImageBlobUrl))
            .WithMessage("Alt text is required when an image is set.");
    }
}

public sealed class UpdateClassSlotRequestValidator : AbstractValidator<UpdateClassSlotRequest>
{
    public UpdateClassSlotRequestValidator()
    {
        RuleFor(x => x.Slug).NotEmpty().MaximumLength(200).Matches("^[a-z0-9-]+$");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.AudienceAgeGroup).NotEmpty().MaximumLength(100);
        RuleFor(x => x.GeneralMeetingTime).MaximumLength(200);
        RuleFor(x => x.DefaultRoom).MaximumLength(200);
        RuleFor(x => x.ImageAltText).MaximumLength(500);
        RuleFor(x => x.ImageAltText)
            .NotEmpty()
            .When(x => !string.IsNullOrWhiteSpace(x.ImageBlobUrl))
            .WithMessage("Alt text is required when an image is set.");
    }
}

public sealed class CreateClassOfferingRequestValidator : AbstractValidator<CreateClassOfferingRequest>
{
    public CreateClassOfferingRequestValidator()
    {
        RuleFor(x => x.ClassSlotId).NotEqual(Guid.Empty);
        RuleFor(x => x.Subject).NotEmpty().MaximumLength(200);
        RuleFor(x => x.TeacherFreeText).MaximumLength(200);
        RuleFor(x => x.MaterialsNeeded).MaximumLength(1000);
        RuleFor(x => x.EndDate).GreaterThanOrEqualTo(x => x.StartDate)
            .WithMessage("End date must be on or after the start date.");
    }
}

public sealed class UpdateClassOfferingRequestValidator : AbstractValidator<UpdateClassOfferingRequest>
{
    public UpdateClassOfferingRequestValidator()
    {
        RuleFor(x => x.ClassSlotId).NotEqual(Guid.Empty);
        RuleFor(x => x.Subject).NotEmpty().MaximumLength(200);
        RuleFor(x => x.TeacherFreeText).MaximumLength(200);
        RuleFor(x => x.MaterialsNeeded).MaximumLength(1000);
        RuleFor(x => x.EndDate).GreaterThanOrEqualTo(x => x.StartDate)
            .WithMessage("End date must be on or after the start date.");
    }
}
