using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Interfaces;
using OmniRoute.Application.Common.Models;

namespace OmniRoute.Application.Features.Auth.Commands.ResendOtp;

internal sealed class ResendOtpCommandHandler : ICommandHandler<ResendOtpCommand>
{
    private readonly IEmailService _emailService;
    private readonly IOTPCacheService _otpCacheService;

    public ResendOtpCommandHandler(
        IEmailService emailService,
        IOTPCacheService otpCacheService)
    {
        _emailService = emailService;
        _otpCacheService = otpCacheService;
    }

    public async Task<Result> Handle(ResendOtpCommand request, CancellationToken cancellationToken)
    {
        var result = await _otpCacheService.ResendOtpAsync(request.Email, cancellationToken);

        if (!result.IsSuccess)
        {
            return result;
        }

        var otp = _otpCacheService.GetLastGeneratedOtp();
        await _emailService.SendOtpEmailAsync(request.Email, otp, cancellationToken);

        return Result.Success();
    }
}

