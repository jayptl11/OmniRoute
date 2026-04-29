using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Features.Stores.DTOs;

namespace OmniRoute.Application.Features.Stores.Commands.CreateStore;

public record CreateStoreCommand(
    string StoreCode,
    string StoreName,
    int MaxCapacity,
    string? Address,
    string? Region,
    string? ManagerUsername) : ICommand<StoreDto>;
