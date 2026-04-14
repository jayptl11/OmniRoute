using OmniRoute.Application.Common.Models;
using MediatR;

namespace OmniRoute.Application.Features.Auth.Commands.Register;

public record RegisterCommand(
    string Email,
    string Username,
    string FirstName,
    string LastName,
    string Password
) : IRequest<Result>;

