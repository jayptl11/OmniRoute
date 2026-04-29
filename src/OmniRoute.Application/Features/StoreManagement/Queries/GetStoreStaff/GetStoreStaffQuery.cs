using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Features.StoreManagement.DTOs;

namespace OmniRoute.Application.Features.StoreManagement.Queries.GetStoreStaff;

public record GetStoreStaffQuery : IQuery<List<StoreStaffDto>>;
