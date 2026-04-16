using OmniRoute.Application.Common.Abstractions;

namespace OmniRoute.Application.Features.Auth.Commands.Register;

public record RegisterCommand(
    string Email,
    string Username,
    string FirstName,
    string LastName,
    string Password
) : ICommand;

