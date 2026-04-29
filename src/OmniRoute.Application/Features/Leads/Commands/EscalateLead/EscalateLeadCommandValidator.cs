using FluentValidation;

namespace OmniRoute.Application.Features.Leads.Commands.EscalateLead;

public class EscalateLeadCommandValidator : AbstractValidator<EscalateLeadCommand>
{
    public EscalateLeadCommandValidator()
    {
        RuleFor(x => x.LeadId)
            .NotEmpty()
            .WithMessage("LeadId không được để trống.");

        RuleFor(x => x.EscalateTo)
            .NotEmpty()
            .WithMessage("Người nhận escalate không được để trống.");

        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("Lý do escalate là bắt buộc.")
            .MaximumLength(500)
            .WithMessage("Lý do không được vượt quá 500 ký tự.");
    }
}
