namespace OmniRoute.Application.Features.Leads.DTOs;

public record EscalateHistoryItemDto(
    Guid LogId,
    Guid LeadId,
    string LeadCode,
    string CustomerName,
    string CustomerPhone,
    Guid EscalateTo,
    string? EscalateToName,
    string? Reason,
    DateTime PerformedAt);
