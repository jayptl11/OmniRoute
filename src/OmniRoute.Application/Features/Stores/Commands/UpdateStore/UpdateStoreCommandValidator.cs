using FluentValidation;

namespace OmniRoute.Application.Features.Stores.Commands.UpdateStore;

public class UpdateStoreCommandValidator : AbstractValidator<UpdateStoreCommand>
{
    public UpdateStoreCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Id is required.");

        RuleFor(x => x.StoreName)
            .NotEmpty().WithMessage("StoreName is required.")
            .MaximumLength(200).WithMessage("StoreName must not exceed 200 characters.");

        RuleFor(x => x.MaxCapacity)
            .GreaterThan(0).WithMessage("MaxCapacity must be greater than 0.");
    }
}
