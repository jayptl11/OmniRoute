using OmniRoute.Application.Common.Models;
using MediatR;

namespace OmniRoute.Application.Features.Auth.Commands.Logout;

public record LogoutCommand(
    string? AccessToken = null,
    string? RefreshToken = null
) : IRequest<Result>;

