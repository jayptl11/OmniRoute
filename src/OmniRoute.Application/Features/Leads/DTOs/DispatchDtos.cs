namespace OmniRoute.Application.Features.Leads.DTOs;

// DP-01 + DP-07: Item trong danh sách lead chờ điều phối
public record PendingDispatchLeadListItemDto(
    Guid LeadId,
    string LeadCode,
    string CustomerName,
    string CustomerPhone,
    string? CustomerAddress,
    string NeedDescription,
    string? NeedType,
    string? PriorityLevel,
    int WaitedMinutes,
    DateTime CreatedAt
);

// DP-02: Chi tiết lead cần phân công
public record PendingDispatchLeadDetailDto(
    Guid LeadId,
    string LeadCode,
    string CustomerName,
    string CustomerPhone,
    string? CustomerAddress,
    string? CustomerEmail,
    string Channel,
    string NeedDescription,
    List<string>? ProductInterest,
    string? NeedType,
    int PriorityScore,
    string? PriorityLevel,
    string? AssignedGroup,
    int WaitedMinutes,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    List<ActivityLogItemDto> ActivityLogs
);

// DP-04: Response khi gán lead về cửa hàng thành công
public record DispatchLeadToStoreResponse(
    Guid LeadId,
    string LeadCode,
    Guid AssignedStoreId,
    string StoreName,
    DateTime AssignedAt,
    DateTime SlaDeadline
);

// DP-06: Item trong lịch sử phân công
public record DispatchHistoryItemDto(
    Guid LeadId,
    string LeadCode,
    string CustomerName,
    string CustomerPhone,
    Guid StoreId,
    string StoreName,
    string? DispatchNote,
    DateTime DispatchedAt,
    string LeadStatus
);
