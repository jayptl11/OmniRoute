using System.Text.Json;
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
