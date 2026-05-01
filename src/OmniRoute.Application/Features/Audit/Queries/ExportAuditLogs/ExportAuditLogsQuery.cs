using OmniRoute.Application.Common.Abstractions;

namespace OmniRoute.Application.Features.Audit.Queries.ExportAuditLogs;

/// <summary>QT-13: Xuất toàn bộ audit log khớp filter ra file Excel.</summary>
public record ExportAuditLogsQuery(
    string? EntityType = null,
    string? Action = null,
    Guid? PerformedBy = null,
    DateTime? DateFrom = null,
    DateTime? DateTo = null) : IQuery<ExportAuditLogsResult>;

public record ExportAuditLogsResult(byte[] FileBytes, string ContentType, string FileName);
