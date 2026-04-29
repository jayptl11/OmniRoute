using OmniRoute.Application.Common.Abstractions;

namespace OmniRoute.Application.Features.Teams.Commands.RemoveTeamMember;

public record RemoveTeamMemberCommand(Guid UserId) : ICommand;
