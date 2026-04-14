using OmniRoute.Application.Common.Models;
using OmniRoute.Application.Features.Auth.DTOs;
using MediatR;

namespace OmniRoute.Application.Features.Auth.Commands.Login;

public record LoginCommand(string Identifier, string Password) : IRequest<Result<LoginResponse>>;

