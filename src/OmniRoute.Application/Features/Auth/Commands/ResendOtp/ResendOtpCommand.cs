using OmniRoute.Application.Common.Abstractions;

namespace OmniRoute.Application.Features.Auth.Commands.ResendOtp;

public record ResendOtpCommand(string Email) : ICommand;

