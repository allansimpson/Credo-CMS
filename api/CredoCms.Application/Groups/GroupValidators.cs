using FluentValidation;

namespace CredoCms.Application.Groups;

public sealed class CreateGroupRequestValidator : AbstractValidator<CreateGroupRequest>
{
    public CreateGroupRequestValidator()
    {
        RuleFor(x => x.Slug).NotEmpty().MaximumLength(200)
            .Matches("^[a-z0-9-]+$")
            .WithMessage("Slug must use lowercase letters, digits, and dashes only.");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ImageAltText).MaximumLength(500);
        RuleFor(x => x.ContactEmail).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.ContactEmail));
        RuleFor(x => x.MeetingInfo).MaximumLength(500);
        RuleFor(x => x.ImageAltText)
            .NotEmpty()
            .When(x => !string.IsNullOrWhiteSpace(x.ImageBlobUrl))
            .WithMessage("Alt text is required when an image is set.");
    }
}

public sealed class UpdateGroupRequestValidator : AbstractValidator<UpdateGroupRequest>
{
    public UpdateGroupRequestValidator()
    {
        RuleFor(x => x.Slug).NotEmpty().MaximumLength(200)
            .Matches("^[a-z0-9-]+$");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ImageAltText).MaximumLength(500);
        RuleFor(x => x.ContactEmail).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.ContactEmail));
        RuleFor(x => x.MeetingInfo).MaximumLength(500);
        RuleFor(x => x.ImageAltText)
            .NotEmpty()
            .When(x => !string.IsNullOrWhiteSpace(x.ImageBlobUrl))
            .WithMessage("Alt text is required when an image is set.");
    }
}

public sealed class JoinRequestRequestValidator : AbstractValidator<JoinRequestRequest>
{
    public JoinRequestRequestValidator()
    {
        // Required-vs-optional is enforced at the service layer (depends on the
        // group's RequiresMessageOnJoinRequest setting); here we only cap length.
        RuleFor(x => x.Message).MaximumLength(1000);
    }
}
