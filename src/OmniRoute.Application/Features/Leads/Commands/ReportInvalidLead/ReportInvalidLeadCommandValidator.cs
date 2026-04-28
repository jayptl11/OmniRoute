using FluentValidation;

namespace OmniRoute.Application.Features.Leads.Commands.ReportInvalidLead;

public class ReportInvalidLeadCommandValidator : AbstractValidator<ReportInvalidLeadCommand>
{
    private static readonly HashSet<string> _validReasons =
    [
        "Spam",
        "WrongPhone",
        "Unreachable",
        "Other"
    ];

    public ReportInvalidLeadCommandValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Lý do báo không hợp lệ là bắt buộc.")
            .Must(r => _validReasons.Contains(r, StringComparer.Ordinal))
            .WithMessage($"Lý do phải là một trong: {string.Join(", ", _validReasons)}.");
    }
}
