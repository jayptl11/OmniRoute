using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.DTOs;

namespace OmniRoute.Application.Features.Leads.Queries.GetTeamReassignTargets;

public record GetTeamReassignTargetsQuery(Guid LeadId, string? Q) : IQuery<List<UserPickerOptionDto>>;
