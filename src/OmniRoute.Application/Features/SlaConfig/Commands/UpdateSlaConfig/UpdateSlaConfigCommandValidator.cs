using FluentValidation;

namespace OmniRoute.Application.Features.SlaConfig.Commands.UpdateSlaConfig;

public class UpdateSlaConfigCommandValidator : AbstractValidator<UpdateSlaConfigCommand>
{
    public UpdateSlaConfigCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Id is required.");

        RuleFor(x => x.MaxHours)
            .GreaterThanOrEqualTo(1).WithMessage("MaxHours must be at least 1.");

        RuleFor(x => x.WarningBeforeHours)
            .GreaterThanOrEqualTo(1).WithMessage("WarningBeforeHours must be at least 1.")
            .LessThan(x => x.MaxHours).WithMessage("WarningBeforeHours must be less than MaxHours.");
    }
}
