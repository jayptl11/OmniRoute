using FluentValidation;
using OmniRoute.Application.Features.Leads.Commands.CreateLead;

namespace OmniRoute.Application.Features.Leads.Commands.CreateLead;

public class CreateLeadCommandValidator : AbstractValidator<CreateLeadCommand>
{
    public CreateLeadCommandValidator()
    {
        RuleFor(x => x.CustomerName)
            .NotEmpty().WithMessage("Tên khách hàng là bắt buộc.");

        RuleFor(x => x.CustomerPhone)
            .NotEmpty().WithMessage("Số điện thoại là bắt buộc.")
            .Matches(@"^0\d{9}$").WithMessage("Số điện thoại không hợp lệ (10 số, bắt đầu bằng 0).");

        RuleFor(x => x.Channel)
            .IsInEnum().WithMessage("Kênh tiếp nhận không hợp lệ.");

        RuleFor(x => x.NeedDescription)
            .NotEmpty().WithMessage("Mô tả nhu cầu là bắt buộc.")
            .MinimumLength(10).WithMessage("Mô tả nhu cầu phải có ít nhất 10 ký tự.");
    }
}
