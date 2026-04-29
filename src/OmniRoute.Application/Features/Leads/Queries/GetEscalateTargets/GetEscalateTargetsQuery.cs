using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Features.Leads.DTOs;

namespace OmniRoute.Application.Features.Leads.Queries.GetEscalateTargets;

public record GetEscalateTargetsQuery(string? Q) : IQuery<List<EscalateTargetDto>>;
