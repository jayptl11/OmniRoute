using FluentValidation;

namespace OmniRoute.Application.Features.AiApiKeys.Commands.AddAiApiKey;

public class AddAiApiKeyCommandValidator : AbstractValidator<AddAiApiKeyCommand>
{
    private static readonly string[] AllowedProviders = ["OpenAI", "Gemini", "Anthropic"];

    public AddAiApiKeyCommandValidator()
    {
        RuleFor(x => x.Provider)
            .NotEmpty().WithMessage("Provider is required.")
            .Must(p => AllowedProviders.Contains(p))
            .WithMessage("Provider must be one of: OpenAI, Gemini, Anthropic.");

        RuleFor(x => x.DisplayName)
            .NotEmpty().WithMessage("Display name is required.")
            .MaximumLength(100).WithMessage("Display name must not exceed 100 characters.");

        RuleFor(x => x.PlainKeyValue)
            .NotEmpty().WithMessage("API key value is required.")
            .MinimumLength(8).WithMessage("API key value is too short.");

        RuleFor(x => x.Priority)
            .InclusiveBetween(1, 2).WithMessage("Priority must be 1 (primary) or 2 (fallback).");
    }
}
