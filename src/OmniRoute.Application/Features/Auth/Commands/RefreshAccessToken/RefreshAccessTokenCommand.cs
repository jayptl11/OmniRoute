using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Features.Auth.DTOs;

namespace OmniRoute.Application.Features.Auth.Commands.RefreshAccessToken;

public record RefreshAccessTokenCommand(string RefreshToken) : ICommand<LoginResponse>;

