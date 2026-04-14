using OmniRoute.Application.Common.Models;
using OmniRoute.Application.Features.Auth.DTOs;
using MediatR;

namespace OmniRoute.Application.Features.Auth.Commands.RefreshAccessToken;

public record RefreshAccessTokenCommand(string RefreshToken) : IRequest<Result<LoginResponse>>;

