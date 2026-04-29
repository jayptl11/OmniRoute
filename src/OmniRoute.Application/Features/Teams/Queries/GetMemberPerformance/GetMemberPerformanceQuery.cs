using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Features.Teams.DTOs;

namespace OmniRoute.Application.Features.Teams.Queries.GetMemberPerformance;

public record GetMemberPerformanceQuery(Guid UserId, string Period = "month")
    : IQuery<MemberPerformanceDto>;
