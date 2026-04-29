using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniRoute.Application.Common.Models;
using OmniRoute.Application.Features.Leads.Commands.AddInternalNote;
using OmniRoute.Application.Features.StoreManagement.Commands.ReassignLeadInStore;
using OmniRoute.Application.Features.StoreManagement.DTOs;
using OmniRoute.Application.Features.StoreManagement.Queries.GetStoreLeadHistory;
using OmniRoute.Application.Features.StoreManagement.Queries.GetStoreLeads;
using OmniRoute.Application.Features.StoreManagement.Queries.GetStoreReport;

namespace OmniRoute.API.Controllers;

[ApiController]
[Route("api/store-leads")]
[Authorize(Policy = "CanManageStore")]
public sealed class StoreLeadsController : ControllerBase
{
    private readonly ISender _sender;

    public StoreLeadsController(ISender sender) => _sender = sender;

    /// <summary>
    /// QL-01 — Xem toàn bộ lead của đơn vị.
    /// Hỗ trợ lọc: search (tên/SĐT), status, priorityLevel, channel, assignedUserId, dateRange.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<StoreLeadListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetStoreLeads(
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] string? priorityLevel,
        [FromQuery] string? channel,
        [FromQuery] Guid? assignedUserId,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var query = new GetStoreLeadsQuery(
            search, status, priorityLevel, channel, assignedUserId, dateFrom, dateTo, page, pageSize);
        var result = await _sender.Send(query, ct);
        if (!result.IsSuccess)
            return BadRequest(new { result.ErrorCode, result.ErrorMessage });
        return Ok(result.Value);
    }

    /// <summary>QL-03 — Reassign lead sang nhân viên khác trong cùng đơn vị.</summary>
    [HttpPatch("{leadId:guid}/reassign")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReassignLead(
        Guid leadId,
        [FromBody] ReassignLeadInStoreRequest request,
        CancellationToken ct)
    {
        var result = await _sender.Send(new ReassignLeadInStoreCommand(leadId, request.NewUserId, request.Reason), ct);
        if (!result.IsSuccess)
        {
            if (result.ErrorCode is "LEAD_NOT_FOUND" or "NEW_USER_NOT_FOUND")
                return NotFound(new { result.ErrorCode, result.ErrorMessage });
            return BadRequest(new { result.ErrorCode, result.ErrorMessage });
        }
        return NoContent();
    }

    /// <summary>
    /// QL-05 — Xem lịch sử xử lý lead theo nhân sự (audit trail).
    /// Lọc tùy chọn: userId, dateFrom, dateTo.
    /// </summary>
    [HttpGet("history")]
    [ProducesResponseType(typeof(PagedResult<StoreLeadHistoryItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetLeadHistory(
        [FromQuery] Guid? userId,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _sender.Send(new GetStoreLeadHistoryQuery(userId, dateFrom, dateTo, page, pageSize), ct);
        if (!result.IsSuccess)
            return BadRequest(new { result.ErrorCode, result.ErrorMessage });
        return Ok(result.Value);
    }

    /// <summary>QL-04 — Báo cáo hiệu quả đơn vị theo kỳ (week / month / quarter hoặc dateRange).</summary>
    [HttpGet("report")]
    [ProducesResponseType(typeof(StoreReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetReport(
        [FromQuery] string period = "month",
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null,
        CancellationToken ct = default)
    {
        var result = await _sender.Send(new GetStoreReportQuery(period, dateFrom, dateTo), ct);
        if (!result.IsSuccess)
            return BadRequest(new { result.ErrorCode, result.ErrorMessage });
        return Ok(result.Value);
    }

    /// <summary>QL ghi chú nội bộ trên lead (chỉ TN/QL/QT xem được).</summary>
    [HttpPost("{leadId:guid}/internal-notes")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddInternalNote(
        Guid leadId,
        [FromBody] StoreLeadInternalNoteRequest request,
        CancellationToken ct)
    {
        var result = await _sender.Send(new AddInternalNoteToLeadCommand(leadId, request.Content), ct);
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "LEAD_NOT_FOUND")
                return NotFound(new { result.ErrorCode, result.ErrorMessage });
            return BadRequest(new { result.ErrorCode, result.ErrorMessage });
        }
        return NoContent();
    }
}

// Request bodies
public record ReassignLeadInStoreRequest(Guid NewUserId, string Reason);
public record StoreLeadInternalNoteRequest(string Content);
