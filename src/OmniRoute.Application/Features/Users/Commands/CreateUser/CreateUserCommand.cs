using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Features.Users.DTOs;

namespace OmniRoute.Application.Features.Users.Commands.CreateUser;

public record CreateUserCommand(
    string Username,
    string Email,
    string? FirstName,
    string? LastName,
    Guid RoleId,
    Guid? StoreId,
    string? Phone) : ICommand<CreateUserResponse>;
