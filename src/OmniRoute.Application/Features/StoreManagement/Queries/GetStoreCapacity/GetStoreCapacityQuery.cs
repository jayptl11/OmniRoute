using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Features.StoreManagement.DTOs;

namespace OmniRoute.Application.Features.StoreManagement.Queries.GetStoreCapacity;

public record GetStoreCapacityQuery : IQuery<StoreCapacityResultDto>;
