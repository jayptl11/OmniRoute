using FluentValidation;
using OmniRoute.Domain.Enums;

namespace OmniRoute.Application.Features.Tickets.Commands.UpdateTicketStatus;

public sealed class UpdateTicketStatusCommandValidator : AbstractValidator<UpdateTicketStatusCommand>
{
    private static readonly HashSet<string> AllowedStatuses =
    [
        nameof(TicketStatus.InProgress),
        nameof(TicketStatus.WaitingCustomer),
        nameof(TicketStatus.Resolved),
        nameof(TicketStatus.Closed)
    ];

    public UpdateTicketStatusCommandValidator()
    {
        RuleFor(x => x.TicketId)
            .NotEmpty().WithMessage("TicketId là bắt buộc.");

        RuleFor(x => x.NewStatus)
            .NotEmpty().WithMessage("NewStatus là bắt buộc.")
            .Must(s => AllowedStatuses.Contains(s))
            .WithMessage($"NewStatus phải là một trong: {string.Join(", ", AllowedStatuses)}.");

        // Note bắt buộc khi chuyển sang InProgress hoặc Resolved
        RuleFor(x => x.Note)
            .NotEmpty()
            .WithMessage("Note là bắt buộc khi chuyển trạng thái sang InProgress hoặc Resolved.")
            .When(x => x.NewStatus is nameof(TicketStatus.InProgress) or nameof(TicketStatus.Resolved));
    }
}
