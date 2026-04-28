using FluentValidation;

namespace OmniRoute.Application.Features.Tickets.Commands.AddTicketNote;

public sealed class AddTicketNoteCommandValidator : AbstractValidator<AddTicketNoteCommand>
{
    public AddTicketNoteCommandValidator()
    {
        RuleFor(x => x.TicketId)
            .NotEmpty().WithMessage("TicketId là bắt buộc.");

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Nội dung ghi chú không được để trống.")
            .MaximumLength(4000).WithMessage("Nội dung ghi chú không vượt quá 4000 ký tự.");
    }
}
