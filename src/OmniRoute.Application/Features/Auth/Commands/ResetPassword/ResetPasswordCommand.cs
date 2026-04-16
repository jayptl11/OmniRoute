using OmniRoute.Application.Common.Abstractions;

namespace OmniRoute.Application.Features.Auth.Commands.ResetPassword;

public record ResetPasswordCommand(string ResetToken, string NewPassword) : ICommand;

