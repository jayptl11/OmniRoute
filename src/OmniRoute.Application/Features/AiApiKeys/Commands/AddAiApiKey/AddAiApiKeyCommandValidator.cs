using System.Text.Json;
using FluentValidation;

namespace OmniRoute.Application.Features.AiApiKeys.Commands.AddAiApiKey;

public class AddAiApiKeyCommandValidator : AbstractValidator<AddAiApiKeyCommand>
{
    private static readonly string[] SupportedProviders =
        ["OpenAI", "Gemini", "Anthropic", "Groq"];

    public AddAiApiKeyCommandValidator()
    {
        RuleFor(x => x.Provider)
            .NotEmpty().WithMessage("Provider is required.")
            .Must(p => SupportedProviders.Contains(p))
            .WithMessage("Provider must be one of: OpenAI, Gemini, Anthropic, Groq.");

        RuleFor(x => x.DisplayName)
            .NotEmpty().WithMessage("Display name is required.")
            .MaximumLength(100).WithMessage("Display name must not exceed 100 characters.");

        RuleFor(x => x.PlainKeyValue)
            .NotEmpty().WithMessage("API key value is required.")
            .MinimumLength(8).WithMessage("API key value is too short.");

        RuleFor(x => x.Priority)
            .GreaterThanOrEqualTo(1).WithMessage("Priority must be >= 1 (lower number = higher priority).");

        RuleFor(x => x.ConfigJson)
            .NotEmpty().WithMessage("ConfigJson is required.")
            .Must(BeValidJson).WithMessage("ConfigJson must be a valid JSON object.")
            .Must(HaveModelField).WithMessage("ConfigJson must contain a non-empty 'model' field.");
    }

    private static bool BeValidJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return false;
        try { JsonDocument.Parse(json); return true; }
        catch { return false; }
    }

    private static bool HaveModelField(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty("model", out var m)
                   && m.ValueKind == JsonValueKind.String
                   && !string.IsNullOrWhiteSpace(m.GetString());
        }
        catch { return false; }
    }
}
