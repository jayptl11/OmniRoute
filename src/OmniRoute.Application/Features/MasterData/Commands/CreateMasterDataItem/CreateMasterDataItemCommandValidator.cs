using FluentValidation;

namespace OmniRoute.Application.Features.MasterData.Commands.CreateMasterDataItem;

public class CreateMasterDataItemCommandValidator : AbstractValidator<CreateMasterDataItemCommand>
{
    public CreateMasterDataItemCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Code is required.")
            .MaximumLength(100).WithMessage("Code must not exceed 100 characters.");

        RuleFor(x => x.DisplayName)
            .NotEmpty().WithMessage("DisplayName is required.")
            .MaximumLength(200).WithMessage("DisplayName must not exceed 200 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(500).When(x => x.Description != null)
            .WithMessage("Description must not exceed 500 characters.");

        RuleFor(x => x.SortOrder)
            .GreaterThanOrEqualTo(0).WithMessage("SortOrder must be >= 0.");
    }
}
