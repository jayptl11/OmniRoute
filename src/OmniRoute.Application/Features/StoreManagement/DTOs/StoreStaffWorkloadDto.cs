namespace OmniRoute.Application.Features.StoreManagement.DTOs;

/// <summary>QL-02 — Workload và tiến độ từng nhân sự trong đơn vị.</summary>
public record StoreStaffWorkloadDto(
    Guid UserId,
    string FullName,
    string? RoleName,
    string? RoleDisplayName,
    bool IsActive,
    int CurrentWorkload,
    int SlaViolatedCount,
    int CompletedCount);
