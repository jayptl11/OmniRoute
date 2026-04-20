using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Models;
using OmniRoute.Application.Features.SlaConfig.DTOs;
using OmniRoute.Domain.Interfaces;

namespace OmniRoute.Application.Features.SlaConfig.Queries.GetSlaConfigs;

internal sealed class GetSlaConfigsQueryHandler : IQueryHandler<GetSlaConfigsQuery, List<SlaConfigDto>>
{
    private readonly ISlaConfigRepository _repository;

    public GetSlaConfigsQueryHandler(ISlaConfigRepository repository)
        => _repository = repository;

    public async Task<Result<List<SlaConfigDto>>> Handle(GetSlaConfigsQuery query, CancellationToken ct)
    {
        var items = await _repository.GetAllAsync(ct);
        var dtos = items.Select(x => new SlaConfigDto(
            x.Id,
            x.AssignedGroup.ToString(),
            x.PriorityLevel.ToString(),
            x.MaxHours,
            x.WarningBeforeHours,
            x.IsActive)).ToList();

        return Result<List<SlaConfigDto>>.Success(dtos);
    }
}
