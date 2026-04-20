using FluentValidation;

namespace OmniRoute.Application.Features.Stores.Commands.CreateStore;

public class CreateStoreCommandValidator : AbstractValidator<CreateStoreCommand>
{
    public CreateStoreCommandValidator()
    {
        RuleFor(x => x.StoreCode)
            .NotEmpty().WithMessage("StoreCode is required.")
            .MaximumLength(20).WithMessage("StoreCode must not exceed 20 characters.");

        RuleFor(x => x.StoreName)
            .NotEmpty().WithMessage("StoreName is required.")
            .MaximumLength(200).WithMessage("StoreName must not exceed 200 characters.");

        RuleFor(x => x.MaxCapacity)
            .GreaterThan(0).WithMessage("MaxCapacity must be greater than 0.");

        RuleFor(x => x.Address)
            .MaximumLength(500).When(x => x.Address != null)
            .WithMessage("Address must not exceed 500 characters.");

        RuleFor(x => x.Region)
            .MaximumLength(100).When(x => x.Region != null)
            .WithMessage("Region must not exceed 100 characters.");
    }
}
