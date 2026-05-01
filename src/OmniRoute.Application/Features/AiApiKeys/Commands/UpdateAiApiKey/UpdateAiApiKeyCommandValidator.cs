using FluentValidation;

namespace OmniRoute.Application.Features.AiApiKeys.Commands.UpdateAiApiKey;

public class UpdateAiApiKeyCommandValidator : AbstractValidator<UpdateAiApiKeyCommand>
{
    public UpdateAiApiKeyCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Id is required.");

        RuleFor(x => x.DisplayName)
            .NotEmpty().WithMessage("Display name is required.")
            .MaximumLength(100).WithMessage("Display name must not exceed 100 characters.");

        When(x => x.PlainKeyValue is not null, () =>
        {
            RuleFor(x => x.PlainKeyValue)
                .MinimumLength(8).WithMessage("API key value is too short.");
        });

        RuleFor(x => x.Priority)
            .InclusiveBetween(1, 2).WithMessage("Priority must be 1 (primary) or 2 (fallback).");
    }
}
