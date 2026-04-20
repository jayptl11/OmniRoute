using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Features.SlaConfig.DTOs;

namespace OmniRoute.Application.Features.SlaConfig.Queries.GetSlaConfigs;

public record GetSlaConfigsQuery : IQuery<List<SlaConfigDto>>;
