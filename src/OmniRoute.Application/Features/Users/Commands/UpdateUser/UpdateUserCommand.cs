using OmniRoute.Application.Common.Abstractions;

namespace OmniRoute.Application.Features.Users.Commands.UpdateUser;

public record UpdateUserCommand(
    Guid UserId,
    string? FirstName,
    string? LastName,
    string Email,
    Guid RoleId,
    Guid? StoreId) : ICommand;
