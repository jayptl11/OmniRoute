using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Features.Teams.DTOs;

namespace OmniRoute.Application.Features.Teams.Queries.GetTeamMembers;

public record GetTeamMembersQuery : IQuery<List<TeamMemberDto>>;
