namespace OmniRoute.Application.Features.StoreManagement.DTOs;

public record StoreStaffDto(
    Guid UserId,
    string FullName,
    string? RoleName,
    string? RoleDisplayName,
    bool IsActive,
    int CurrentWorkload,
    DateTime? LastAssignedAt);
