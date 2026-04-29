using FluentValidation;

namespace OmniRoute.Application.Features.Tickets.Commands.AddInternalNote;

public class AddInternalNoteToTicketCommandValidator : AbstractValidator<AddInternalNoteToTicketCommand>
{
    public AddInternalNoteToTicketCommandValidator()
    {
        RuleFor(x => x.TicketId)
            .NotEmpty()
            .WithMessage("TicketId không được để trống.");

        RuleFor(x => x.Content)
            .NotEmpty()
            .WithMessage("Nội dung ghi chú không được để trống.")
            .MaximumLength(2000)
            .WithMessage("Nội dung ghi chú không được vượt quá 2000 ký tự.");
    }
}
