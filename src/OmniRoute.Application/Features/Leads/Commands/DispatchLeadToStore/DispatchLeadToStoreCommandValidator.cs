using FluentValidation;

namespace OmniRoute.Application.Features.Leads.Commands.DispatchLeadToStore;

public sealed class DispatchLeadToStoreCommandValidator : AbstractValidator<DispatchLeadToStoreCommand>
{
    public DispatchLeadToStoreCommandValidator()
    {
        RuleFor(x => x.LeadId)
            .NotEmpty()
            .WithMessage("LeadId là bắt buộc.");

        RuleFor(x => x.StoreId)
            .NotEmpty()
            .WithMessage("StoreId là bắt buộc.");

        RuleFor(x => x.Note)
            .MaximumLength(500)
            .When(x => x.Note is not null)
            .WithMessage("Ghi chú không được vượt quá 500 ký tự.");
    }
}
