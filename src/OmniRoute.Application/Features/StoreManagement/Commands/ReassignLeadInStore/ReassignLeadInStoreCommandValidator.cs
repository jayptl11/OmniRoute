using FluentValidation;

namespace OmniRoute.Application.Features.StoreManagement.Commands.ReassignLeadInStore;

public class ReassignLeadInStoreCommandValidator : AbstractValidator<ReassignLeadInStoreCommand>
{
    public ReassignLeadInStoreCommandValidator()
    {
        RuleFor(x => x.LeadId)
            .NotEmpty().WithMessage("LeadId là bắt buộc.");

        RuleFor(x => x.NewUserId)
            .NotEmpty().WithMessage("NewUserId là bắt buộc.");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Lý do reassign là bắt buộc.")
            .MaximumLength(500).WithMessage("Lý do không được vượt quá 500 ký tự.");
    }
}
