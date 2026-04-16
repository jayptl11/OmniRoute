using OmniRoute.Application.Common.Abstractions;

namespace OmniRoute.Application.Features.Auth.Commands.Logout;

public record LogoutCommand(
    string? AccessToken = null,
    string? RefreshToken = null
) : ICommand;

