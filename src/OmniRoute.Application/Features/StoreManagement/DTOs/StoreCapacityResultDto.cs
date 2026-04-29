namespace OmniRoute.Application.Features.StoreManagement.DTOs;

/// <summary>QL-09 — Năng lực tiếp nhận của cửa hàng.</summary>
public record StoreCapacityResultDto(
    Guid StoreId,
    string StoreCode,
    string StoreName,
    string? Address,
    string? Region,
    int MaxCapacity,
    int ActiveLeads,
    int AvailableSlots,
    bool IsOverCapacity,
    bool IsNearCapacity);
