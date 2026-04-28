using FluentValidation;

namespace OmniRoute.Application.Features.Tickets.Commands.EscalateTicket;

public sealed class EscalateTicketCommandValidator : AbstractValidator<EscalateTicketCommand>
{
    public EscalateTicketCommandValidator()
    {
        RuleFor(x => x.TicketId)
            .NotEmpty().WithMessage("TicketId là bắt buộc.");

        RuleFor(x => x.EscalateTo)
            .NotEmpty().WithMessage("EscalateTo (người nhận) là bắt buộc.");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Lý do escalate là bắt buộc.")
            .MaximumLength(1000).WithMessage("Lý do không vượt quá 1000 ký tự.");
    }
}
