using FluentValidation;

namespace OmniRoute.Application.Features.Leads.Commands.CreateFollowUpTask;

public class CreateFollowUpTaskCommandValidator : AbstractValidator<CreateFollowUpTaskCommand>
{
    public CreateFollowUpTaskCommandValidator()
    {
        RuleFor(x => x.DueAt)
            .Must(d => d > DateTime.UtcNow)
            .WithMessage("Thời gian hẹn follow-up phải ở trong tương lai.");

        RuleFor(x => x.Note)
            .NotEmpty().WithMessage("Nội dung nhắc nhở là bắt buộc.")
            .MaximumLength(500).WithMessage("Nội dung không được vượt quá 500 ký tự.");
    }
}
