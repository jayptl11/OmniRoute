using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Features.Users.DTOs;

namespace OmniRoute.Application.Features.Users.Commands.ToggleUserStatus;

public record ToggleUserStatusCommand(Guid UserId, bool IsActive) : ICommand<ToggleUserStatusResponse>;
