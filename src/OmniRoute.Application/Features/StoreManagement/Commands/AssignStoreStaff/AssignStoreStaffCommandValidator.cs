using FluentValidation;

namespace OmniRoute.Application.Features.StoreManagement.Commands.AssignStoreStaff;

public class AssignStoreStaffCommandValidator : AbstractValidator<AssignStoreStaffCommand>
{
    public AssignStoreStaffCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId là bắt buộc.");
    }
}
