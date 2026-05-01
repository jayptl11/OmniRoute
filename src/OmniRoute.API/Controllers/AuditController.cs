using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniRoute.Application.Common.Models;
using OmniRoute.Application.Features.Audit.DTOs;
using OmniRoute.Application.Features.Audit.Queries.ExportAuditLogs;
using OmniRoute.Application.Features.Audit.Queries.GetAuditLogs;
using OmniRoute.Application.Features.SystemStats.DTOs;
using OmniRoute.Application.Features.SystemStats.Queries.GetSystemStats;

namespace OmniRoute.API.Controllers;

[ApiController]
[Route("api/audit")]
[Authorize(Policy = "CanAdminSystem")]
public sealed class AuditController : ControllerBase
{
    private readonly ISender _sender;

    public AuditController(ISender sender) => _sender = sender;

    /// <summary>QT-71 — Xem log hệ thống và audit trail. Bộ lọc: entityType, action, performedBy, dateFrom, dateTo.</summary>
    [HttpGet("logs")]
    [ProducesResponseType(typeof(PagedResult<AuditLogDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAuditLogs(
        [FromQuery] string? entityType = null,
        [FromQuery] string? action = null,
        [FromQuery] Guid? performedBy = null,
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _sender.Send(
            new GetAuditLogsQuery(entityType, action, performedBy, dateFrom, dateTo, page, pageSize), ct);

        if (!result.IsSuccess)
            return BadRequest(new { result.ErrorCode, result.ErrorMessage });

        return Ok(result.Value);
    }

    /// <summary>QT-13 — Xuất audit log ra file Excel. Cùng bộ lọc với GET /logs nhưng không phân trang.</summary>
    [HttpGet("logs/export")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExportAuditLogs(
        [FromQuery] string? entityType = null,
        [FromQuery] string? action = null,
        [FromQuery] Guid? performedBy = null,
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null,
        CancellationToken ct = default)
    {
        var result = await _sender.Send(
            new ExportAuditLogsQuery(entityType, action, performedBy, dateFrom, dateTo), ct);

        if (!result.IsSuccess)
            return BadRequest(new { result.ErrorCode, result.ErrorMessage });

        return File(result.Value!.FileBytes, result.Value.ContentType, result.Value.FileName);
    }

    /// <summary>QT-70 — Xem thống kê hoạt động hệ thống. Period: week | month | quarter.</summary>
    [HttpGet("system-stats")]
    [ProducesResponseType(typeof(SystemStatsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetSystemStats(
        [FromQuery] string period = "month",
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null,
        CancellationToken ct = default)
    {
        var result = await _sender.Send(new GetSystemStatsQuery(period, dateFrom, dateTo), ct);

        if (!result.IsSuccess)
            return BadRequest(new { result.ErrorCode, result.ErrorMessage });

        return Ok(result.Value);
    }
}
