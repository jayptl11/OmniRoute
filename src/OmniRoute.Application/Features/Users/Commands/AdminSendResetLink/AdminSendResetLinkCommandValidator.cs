using FluentValidation;

namespace OmniRoute.Application.Features.Users.Commands.AdminSendResetLink;

public class AdminSendResetLinkCommandValidator : AbstractValidator<AdminSendResetLinkCommand>
{
    public AdminSendResetLinkCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required.");
    }
}
