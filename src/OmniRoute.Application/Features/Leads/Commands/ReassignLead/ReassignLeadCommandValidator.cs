using FluentValidation;

namespace OmniRoute.Application.Features.Leads.Commands.ReassignLead;

public class ReassignLeadCommandValidator : AbstractValidator<ReassignLeadCommand>
{
    public ReassignLeadCommandValidator()
    {
        RuleFor(x => x.LeadId)
            .NotEmpty()
            .WithMessage("LeadId không được để trống.");

        RuleFor(x => x.NewUserId)
            .NotEmpty()
            .WithMessage("NewUserId không được để trống.");

        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("Lý do reassign là bắt buộc.")
            .MaximumLength(500)
            .WithMessage("Lý do không được vượt quá 500 ký tự.");
    }
}
