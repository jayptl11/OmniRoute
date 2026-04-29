using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Features.StoreManagement.DTOs;

namespace OmniRoute.Application.Features.StoreManagement.Queries.GetStoreStaffWorkload;

public record GetStoreStaffWorkloadQuery : IQuery<List<StoreStaffWorkloadDto>>;
