namespace OmniRoute.Application.Features.StoreManagement.DTOs;

public record StoreStaffDto(
    Guid UserId,
    string FullName,
    string? RoleName,
    bool IsActive,
    int CurrentWorkload,
    DateTime? LastAssignedAt);
