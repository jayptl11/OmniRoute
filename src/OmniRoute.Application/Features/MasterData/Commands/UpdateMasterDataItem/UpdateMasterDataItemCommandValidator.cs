using FluentValidation;

namespace OmniRoute.Application.Features.MasterData.Commands.UpdateMasterDataItem;

public class UpdateMasterDataItemCommandValidator : AbstractValidator<UpdateMasterDataItemCommand>
{
    public UpdateMasterDataItemCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Id is required.");

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
