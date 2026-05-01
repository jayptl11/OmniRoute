using OmniRoute.Application.Common.Abstractions;
using OmniRoute.Application.Common.Models;
using OmniRoute.Application.Features.Audit.DTOs;

namespace OmniRoute.Application.Features.Audit.Queries.GetAuditLogs;

public record GetAuditLogsQuery(
    string? EntityType = null,
    string? Action = null,
    Guid? PerformedBy = null,
    DateTime? DateFrom = null,
    DateTime? DateTo = null,
    int Page = 1,
    int PageSize = 20) : IQuery<PagedResult<AuditLogDto>>;
