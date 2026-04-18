using FluentValidation;

namespace OmniRoute.Application.Features.Users.Commands.UpdateUser;

public class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email is required.");

        RuleFor(x => x.RoleId)
            .NotEmpty().WithMessage("Role is required.");

        RuleFor(x => x.FirstName)
            .MaximumLength(100).When(x => x.FirstName != null)
            .WithMessage("First name must not exceed 100 characters.");

        RuleFor(x => x.LastName)
            .MaximumLength(100).When(x => x.LastName != null)
            .WithMessage("Last name must not exceed 100 characters.");
    }
}
