using OmniRoute.Application.Common.Abstractions;

namespace OmniRoute.Application.Features.Stores.Commands.UpdateStore;

public record UpdateStoreCommand(
    Guid Id,
    string StoreName,
    int MaxCapacity,
    string? Address,
    string? Region,
    string? ManagerUsername) : ICommand;
