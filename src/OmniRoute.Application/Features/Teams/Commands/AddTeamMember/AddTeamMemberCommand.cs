using OmniRoute.Application.Common.Abstractions;

namespace OmniRoute.Application.Features.Teams.Commands.AddTeamMember;

public record AddTeamMemberCommand(Guid UserId) : ICommand;
