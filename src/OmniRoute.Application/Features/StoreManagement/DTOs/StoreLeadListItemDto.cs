namespace OmniRoute.Application.Features.StoreManagement.DTOs;

/// <summary>QL-01 — Một dòng trong danh sách lead của đơn vị.</summary>
public record StoreLeadListItemDto(
    Guid LeadId,
    string LeadCode,
    string CustomerName,
    string CustomerPhone,
    string? NeedType,
    string LeadStatus,
    string? PriorityLevel,
    DateTime? SlaDeadline,
    bool SlaViolated,
    Guid? AssignedUserId,
    string? AssignedUserName);
