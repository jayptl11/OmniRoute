using FluentValidation;

namespace OmniRoute.Application.Features.RoutingRules.Commands.UpdateRoutingRule;

public class UpdateRoutingRuleCommandValidator : AbstractValidator<UpdateRoutingRuleCommand>
{
    public UpdateRoutingRuleCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Rule ID is required.");

        RuleFor(x => x.RuleName)
            .NotEmpty().WithMessage("Rule name is required.")
            .MaximumLength(200).WithMessage("Rule name must not exceed 200 characters.");

        RuleFor(x => x.PriorityOrder)
            .GreaterThan(0).WithMessage("Priority order must be greater than 0.");

        RuleFor(x => x.ActionGroup)
            .IsInEnum().WithMessage("Action group must be a valid value.");
    }
}
