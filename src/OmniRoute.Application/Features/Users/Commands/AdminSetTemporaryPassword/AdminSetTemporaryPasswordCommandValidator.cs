using FluentValidation;

namespace OmniRoute.Application.Features.Users.Commands.AdminSetTemporaryPassword;

public class AdminSetTemporaryPasswordCommandValidator : AbstractValidator<AdminSetTemporaryPasswordCommand>
{
    public AdminSetTemporaryPasswordCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required.");

        RuleFor(x => x.TemporaryPassword)
            .NotEmpty().WithMessage("Temporary password is required.")
            .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$")
            .WithMessage("Password must be at least 8 characters and contain at least one uppercase letter, one lowercase letter, and one digit.");
    }
}
