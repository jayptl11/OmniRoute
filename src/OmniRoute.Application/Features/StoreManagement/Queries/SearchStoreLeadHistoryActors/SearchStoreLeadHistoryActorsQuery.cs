using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.DTOs;

namespace OmniRoute.Application.Features.StoreManagement.Queries.SearchStoreLeadHistoryActors;

public record SearchStoreLeadHistoryActorsQuery(string? Q) : IQuery<List<UserPickerOptionDto>>;
