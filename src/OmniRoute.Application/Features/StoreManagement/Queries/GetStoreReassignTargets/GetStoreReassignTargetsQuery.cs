using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.DTOs;

namespace OmniRoute.Application.Features.StoreManagement.Queries.GetStoreReassignTargets;

public record GetStoreReassignTargetsQuery(Guid LeadId, string? Q) : IQuery<List<UserPickerOptionDto>>;
