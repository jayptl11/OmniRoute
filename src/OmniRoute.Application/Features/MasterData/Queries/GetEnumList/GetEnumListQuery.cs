using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Features.MasterData.DTOs;

namespace OmniRoute.Application.Features.MasterData.Queries.GetEnumList;

/// <summary>
/// Returns the values of a system enum (Channel, NeedType, LeadStatus) as a read-only list.
/// EnumType: "Channel" | "NeedType" | "LeadStatus"
/// </summary>
public record GetEnumListQuery(string EnumType) : IQuery<List<EnumListItemDto>>;
