using OmniRoute.Application.Common.Abstractions;

namespace OmniRoute.Application.Features.Auth.Commands.ForgotPassword;

public record ForgotPasswordCommand(string Email) : ICommand;

