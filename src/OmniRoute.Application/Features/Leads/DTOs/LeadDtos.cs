using OmniRoute.Domain.Enums;

namespace OmniRoute.Application.Features.Leads.DTOs;

public record CreateLeadResponse(
    Guid LeadId,
    string LeadCode,
    bool IsDuplicate,
    Guid? ExistingLeadId,
    string? ExistingLeadCode,
    string? ExistingLeadStatus
);

// TV-02: Kiểm tra duplicate realtime
public record DuplicateCheckDto(
    bool HasDuplicate,
    Guid? ExistingLeadId,
    string? ExistingLeadCode,
    string? ExistingLeadStatus,
    DateTime? ExistingLeadCreatedAt
);

// TV-05 / TV-07: Danh sách lead + tìm kiếm
public record LeadListItemDto(
    Guid LeadId,
    string LeadCode,
    string CustomerName,
    string CustomerPhone,
    string Channel,
    string? NeedType,
    string LeadStatus,
    string? PriorityLevel,
    DateTime CreatedAt
);

// TV-06 / TV-03: Chi tiết lead (bao gồm kết quả phân loại tự động)
public record LeadDetailDto(
    Guid LeadId,
    string LeadCode,
    string CustomerName,
    string CustomerPhone,
    string? CustomerAddress,
    string? CustomerEmail,
    string Channel,
    string NeedDescription,
    List<string>? ProductInterest,
    // Kết quả phân loại (SYS-01 → SYS-03)
    string? NeedType,
    int PriorityScore,
    string? PriorityLevel,
    string? AssignedGroup,
    string? RoutingType,
    // Gán nhân viên
    Guid? AssignedUserId,
    string? AssignedUserName,
    Guid? AssignedStoreId,
    DateTime? AssignedAt,
    // SLA
    DateTime? SlaDeadline,
    bool SlaViolated,
    // Trạng thái
    string LeadStatus,
    // Metadata
    Guid CreatedBy,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? ClosedAt
);

// TV-04: Response khi cập nhật lead
public record UpdateLeadResponse(
    Guid LeadId,
    string LeadCode,
    DateTime UpdatedAt
);

// SA-02: Activity log entry hiển thị trong timeline lead
public record ActivityLogItemDto(
    Guid Id,
    string Action,
    string? Note,
    string? NewValue,
    DateTime PerformedAt,
    string? PerformedByName
);

// SA-01 / SA-03: Danh sách lead được gán cho Sale
public record SaleLeadListItemDto(
    Guid LeadId,
    string LeadCode,
    string CustomerName,
    string CustomerPhone,
    string? NeedType,
    string LeadStatus,
    string? PriorityLevel,
    DateTime? SlaDeadline,
    bool SlaViolated,
    DateTime? AssignedAt
);

// SA-02: Chi tiết lead Sale (bao gồm timeline activity)
public record SaleLeadDetailDto(
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
    string? RoutingType,
    Guid? AssignedUserId,
    string? AssignedUserName,
    Guid? AssignedStoreId,
    DateTime? AssignedAt,
    DateTime? SlaDeadline,
    bool SlaViolated,
    string LeadStatus,
    Guid CreatedBy,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? ClosedAt,
    List<ActivityLogItemDto> ActivityLogs
);

// SA-04: Response khi cập nhật trạng thái lead
public record UpdateLeadStatusResponse(
    Guid LeadId,
    string LeadCode,
    string NewStatus,
    DateTime UpdatedAt
);

// SA-05: Response khi thêm ghi chú tư vấn
public record AddLeadNoteResponse(
    Guid NoteId,
    Guid LeadId,
    DateTime CreatedAt
);
