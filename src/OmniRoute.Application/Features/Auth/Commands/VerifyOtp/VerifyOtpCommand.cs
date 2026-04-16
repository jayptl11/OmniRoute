using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Features.Auth.DTOs;

namespace OmniRoute.Application.Features.Auth.Commands.VerifyOtp;

public record VerifyOtpCommand(
    string Email,
    string Otp
) : ICommand<VerifyOtpResponse>;

