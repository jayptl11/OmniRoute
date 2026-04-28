using FluentValidation;

namespace OmniRoute.Application.Features.Leads.Commands.UpdateLead;

public class UpdateLeadCommandValidator : AbstractValidator<UpdateLeadCommand>
{
    public UpdateLeadCommandValidator()
    {
        RuleFor(x => x.LeadId)
            .NotEmpty()
            .WithMessage("LeadId là bắt buộc.");

        When(x => x.CustomerEmail is not null, () =>
        {
            RuleFor(x => x.CustomerEmail)
                .EmailAddress()
                .WithMessage("Email không hợp lệ.");
        });

        When(x => x.NeedDescription is not null, () =>
        {
            RuleFor(x => x.NeedDescription)
                .MinimumLength(10)
                .WithMessage("Mô tả nhu cầu phải có ít nhất 10 ký tự.");
        });
    }
}
