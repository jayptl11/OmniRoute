using FluentValidation;
using OmniRoute.Domain.Enums;

namespace OmniRoute.Application.Features.Leads.Commands.UpdateLeadStatus;

public class UpdateLeadStatusCommandValidator : AbstractValidator<UpdateLeadStatusCommand>
{
    // Các trạng thái Sale được phép chuyển sang (BR-05)
    private static readonly HashSet<string> _validStatuses =
    [
        nameof(LeadStatus.Contacted),
        nameof(LeadStatus.InProgress),
        nameof(LeadStatus.Won),
        nameof(LeadStatus.Lost),
        nameof(LeadStatus.Cancelled)
    ];

    public UpdateLeadStatusCommandValidator()
    {
        RuleFor(x => x.NewStatus)
            .NotEmpty().WithMessage("Trạng thái mới là bắt buộc.")
            .Must(s => _validStatuses.Contains(s, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Trạng thái phải là một trong: {string.Join(", ", _validStatuses)}.");

        // Ghi chú bắt buộc khi chuyển sang Contacted hoặc InProgress
        RuleFor(x => x.Note)
            .NotEmpty()
            .WithMessage("Ghi chú tư vấn là bắt buộc khi chuyển sang trạng thái này.")
            .When(x => x.NewStatus.Equals(nameof(LeadStatus.Contacted), StringComparison.OrdinalIgnoreCase) ||
                       x.NewStatus.Equals(nameof(LeadStatus.InProgress), StringComparison.OrdinalIgnoreCase));

        // Lý do bắt buộc khi đóng Lost
        RuleFor(x => x.LostReason)
            .NotEmpty()
            .WithMessage("Lý do lost là bắt buộc khi đóng lead với trạng thái Lost.")
            .When(x => x.NewStatus.Equals(nameof(LeadStatus.Lost), StringComparison.OrdinalIgnoreCase));

        // Lý do bắt buộc khi hủy
        RuleFor(x => x.CancelReason)
            .NotEmpty()
            .WithMessage("Lý do hủy là bắt buộc khi chuyển sang trạng thái Cancelled.")
            .When(x => x.NewStatus.Equals(nameof(LeadStatus.Cancelled), StringComparison.OrdinalIgnoreCase));
    }
}
