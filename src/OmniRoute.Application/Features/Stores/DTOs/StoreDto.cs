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

// DP-03: Tình trạng cửa hàng kèm workload hiện tại
public record StoreCapacityDto(
    Guid Id,
    string StoreCode,
    string StoreName,
    string? Address,
    string? Region,
    Guid? ManagerId,
    int MaxCapacity,
    int ActiveLeads,
    int AvailableSlots,
    bool IsOverCapacity,
    bool IsNearCapacity,
    bool IsActive
);
