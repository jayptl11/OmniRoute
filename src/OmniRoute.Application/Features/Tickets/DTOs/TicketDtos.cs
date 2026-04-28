namespace OmniRoute.Application.Features.Tickets.DTOs;

// CS-01 / CS-03: row trong danh sách ticket được gán
public record TicketListItemDto(
    Guid TicketId,
    string TicketCode,
    string CustomerName,
    string CustomerPhone,
    string? NeedType,
    string TicketStatus,
    string? PriorityLevel,
    DateTime? SlaDeadline,
    bool SlaViolated,
    DateTime? AssignedAt);

// CS-02: chi tiết ticket (bao gồm activity timeline + lịch sử KH)
public record TicketDetailDto(
    Guid TicketId,
    string TicketCode,
    string CustomerName,
    string CustomerPhone,
    string? CustomerAddress,
    string? CustomerEmail,
    string Channel,
    string NeedDescription,
    string? NeedType,
    int PriorityScore,
    string? PriorityLevel,
    Guid? AssignedUserId,
    string? AssignedUserName,
    Guid? AssignedStoreId,
    DateTime? AssignedAt,
    DateTime? SlaDeadline,
    bool SlaViolated,
    string TicketStatus,
    bool IsEscalated,
    string? EscalatedReason,
    int? SatisfactionScore,
    string? SatisfactionNote,
    Guid CreatedBy,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? ClosedAt,
    List<TicketActivityLogItemDto> ActivityLogs,
    List<CustomerTicketHistoryItemDto> CustomerTicketHistory);

// Activity log entry trong timeline của ticket
public record TicketActivityLogItemDto(
    Guid Id,
    string Action,
    string? Note,
    string? NewValue,
    DateTime PerformedAt,
    string? PerformedByName);

// Lịch sử ticket trước của cùng một khách hàng (CS-02)
public record CustomerTicketHistoryItemDto(
    Guid TicketId,
    string TicketCode,
    string? NeedType,
    string TicketStatus,
    DateTime CreatedAt,
    DateTime? ClosedAt);

// CS-04: response khi cập nhật trạng thái
public record UpdateTicketStatusResponse(
    Guid TicketId,
    string TicketCode,
    string NewStatus,
    DateTime UpdatedAt);

// CS-05: response khi thêm ghi chú
public record AddTicketNoteResponse(
    Guid NoteId,
    Guid TicketId,
    DateTime CreatedAt);

// CS-06: response khi escalate
public record EscalateTicketResponse(
    Guid TicketId,
    string TicketCode,
    Guid EscalatedTo,
    DateTime EscalatedAt);

// CS-07: response khi ghi nhận hài lòng
public record RecordSatisfactionResponse(
    Guid TicketId,
    string TicketCode,
    int SatisfactionScore,
    DateTime UpdatedAt);

// CS-08: hiệu suất cá nhân nhân viên CS
public record TicketPerformanceDto(
    string Period,
    DateTime PeriodStart,
    DateTime PeriodEnd,
    int TotalAssigned,
    int TotalProcessed,
    int ResolvedCount,
    double? OnTimeRate,
    double? AvgHandlingTimeMinutes,
    double? AvgSatisfactionScore,
    int SlaViolatedCount,
    DateTime GeneratedAt);
