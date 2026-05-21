using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Models;
using OmniRoute.Application.Features.MasterData.DTOs;
using OmniRoute.Domain.Enums;
using OmniRoute.Domain.Services;

namespace OmniRoute.Application.Features.MasterData.Queries.GetEnumList;

internal sealed class GetEnumListQueryHandler : IQueryHandler<GetEnumListQuery, List<EnumListItemDto>>
{
    private static readonly Dictionary<string, Func<List<EnumListItemDto>>> _enumProviders =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Channel"] = () => Enum.GetValues<Channel>()
                .Select(v => new EnumListItemDto(
                    RoutingRuleChannelHelper.GetCanonicalName(v),
                    RoutingRuleChannelHelper.GetDisplayName(v)))
                .ToList(),

            ["NeedType"] = () => Enum.GetValues<NeedType>()
                .Select(v => new EnumListItemDto(v.ToString(), v.ToString())).ToList(),

            ["LeadStatus"] = () => Enum.GetValues<LeadStatus>()
                .Select(v => new EnumListItemDto(v.ToString(), v.ToString())).ToList(),
        };

    public Task<Result<List<EnumListItemDto>>> Handle(GetEnumListQuery query, CancellationToken ct)
    {
        if (!_enumProviders.TryGetValue(query.EnumType, out var provider))
            return Task.FromResult(Result<List<EnumListItemDto>>.Failure(
                "INVALID_ENUM_TYPE",
                $"Enum type '{query.EnumType}' is not supported. Valid values: Channel, NeedType, LeadStatus."));

        return Task.FromResult(Result<List<EnumListItemDto>>.Success(provider()));
    }
}
