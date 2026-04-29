namespace OmniRoute.Application.Features.StoreManagement.DTOs;

/// <summary>QL-05 — Một dòng trong lịch sử xử lý lead của đơn vị.</summary>
public record StoreLeadHistoryItemDto(
    Guid LogId,
    Guid LeadId,
    string? LeadCode,
    string? CustomerName,
    string? CustomerPhone,
    string Action,
    string? OldValue,
    string? NewValue,
    string? Note,
    Guid? PerformedBy,
    string? PerformedByName,
    DateTime PerformedAt);
