using FluentValidation;

namespace OmniRoute.Application.Features.Leads.Commands.AddInternalNote;

public class AddInternalNoteToLeadCommandValidator : AbstractValidator<AddInternalNoteToLeadCommand>
{
    public AddInternalNoteToLeadCommandValidator()
    {
        RuleFor(x => x.LeadId)
            .NotEmpty()
            .WithMessage("LeadId không được để trống.");

        RuleFor(x => x.Content)
            .NotEmpty()
            .WithMessage("Nội dung ghi chú không được để trống.")
            .MaximumLength(2000)
            .WithMessage("Nội dung ghi chú không được vượt quá 2000 ký tự.");
    }
}
