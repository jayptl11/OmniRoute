namespace OmniRoute.Application.Features.Stores.DTOs;

public record StoreDto(
    Guid Id,
    string StoreCode,
    string StoreName,
    string? Address,
    string? Region,
    Guid? ManagerId,
    int MaxCapacity,
    bool IsActive,
    DateTime CreatedAt);
