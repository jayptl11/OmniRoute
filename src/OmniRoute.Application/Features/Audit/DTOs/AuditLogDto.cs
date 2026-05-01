namespace OmniRoute.Application.Features.Audit.DTOs;

public record AuditLogDto(
    Guid Id,
    string EntityType,
    Guid EntityId,
    string Action,
    string? OldValue,
    string? NewValue,
    string? Note,
    Guid? PerformedBy,
    string? PerformedByName,
    bool IsInternal,
    DateTime PerformedAt);
