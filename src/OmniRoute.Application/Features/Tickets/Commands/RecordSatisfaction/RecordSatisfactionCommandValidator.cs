using FluentValidation;

namespace OmniRoute.Application.Features.Tickets.Commands.RecordSatisfaction;

public sealed class RecordSatisfactionCommandValidator : AbstractValidator<RecordSatisfactionCommand>
{
    public RecordSatisfactionCommandValidator()
    {
        RuleFor(x => x.TicketId)
            .NotEmpty().WithMessage("TicketId là bắt buộc.");

        RuleFor(x => x.Score)
            .InclusiveBetween(1, 5).WithMessage("Điểm hài lòng phải từ 1 đến 5.");

        RuleFor(x => x.Note)
            .MaximumLength(1000).WithMessage("Ghi chú không vượt quá 1000 ký tự.")
            .When(x => x.Note is not null);
    }
}
