using FluentValidation;

namespace OmniRoute.Application.Features.Leads.Commands.AddLeadNote;

public class AddLeadNoteCommandValidator : AbstractValidator<AddLeadNoteCommand>
{
    public AddLeadNoteCommandValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Nội dung ghi chú là bắt buộc.")
            .MaximumLength(2000).WithMessage("Nội dung ghi chú không được vượt quá 2000 ký tự.");
    }
}
