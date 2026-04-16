using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Features.Auth.DTOs;

namespace OmniRoute.Application.Features.Auth.Commands.Login;

public record LoginCommand(string Identifier, string Password) : ICommand<LoginResponse>;

