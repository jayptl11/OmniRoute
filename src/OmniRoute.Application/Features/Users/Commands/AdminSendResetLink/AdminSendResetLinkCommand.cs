using OmniRoute.Application.Common.Abstractions;

namespace OmniRoute.Application.Features.Users.Commands.AdminSendResetLink;

public record AdminSendResetLinkCommand(Guid UserId) : ICommand;
