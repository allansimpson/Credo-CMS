using CredoCms.Domain.Common;
using FluentValidation;

namespace CredoCms.Application.UserManagement;

public sealed class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(254);
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Roles)
            .NotNull()
            .Must(roles => roles.All(r => SystemConstants.Roles.All.Contains(r)))
            .WithMessage("One or more roles are not recognized.");
    }
}

public sealed class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(254);
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Roles)
            .NotNull()
            .Must(roles => roles.All(r => SystemConstants.Roles.All.Contains(r)));
    }
}
