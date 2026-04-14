using OmniRoute.Application.Common.Models;
using OmniRoute.Application.Features.Auth.DTOs;
using MediatR;

namespace OmniRoute.Application.Features.Auth.Commands.VerifyOtp;

public record VerifyOtpCommand(
    string Email,
    string Otp
) : IRequest<Result<VerifyOtpResponse>>;

