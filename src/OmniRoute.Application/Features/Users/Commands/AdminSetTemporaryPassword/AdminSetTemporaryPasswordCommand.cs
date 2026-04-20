using OmniRoute.Application.Common.Abstractions;

namespace OmniRoute.Application.Features.Users.Commands.AdminSetTemporaryPassword;

public record AdminSetTemporaryPasswordCommand(
    Guid UserId,
    string TemporaryPassword) : ICommand;
