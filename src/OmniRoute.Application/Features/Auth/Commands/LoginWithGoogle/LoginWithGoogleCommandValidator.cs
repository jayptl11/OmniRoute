using FluentValidation;

namespace OmniRoute.Application.Features.Auth.Commands.LoginWithGoogle;

public class LoginWithGoogleCommandValidator : AbstractValidator<LoginWithGoogleCommand>
{
    public LoginWithGoogleCommandValidator()
    {
        RuleFor(x => x.IdToken)
            .NotEmpty()
            .WithMessage("IdToken is required")
            .WithErrorCode("ID_TOKEN_REQUIRED");
    }
}

