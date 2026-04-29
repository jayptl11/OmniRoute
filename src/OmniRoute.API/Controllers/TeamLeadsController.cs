using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniRoute.Application.Common.Models;
using OmniRoute.Application.Features.Leads.Commands.EscalateLead;
using OmniRoute.Application.Features.Leads.Commands.AddInternalNote;
using OmniRoute.Application.Features.Leads.Commands.ReassignLead;
using OmniRoute.Application.Features.Leads.DTOs;
using OmniRoute.Application.Features.Leads.Queries.GetEscalateHistory;
using OmniRoute.Application.Features.Leads.Queries.GetSlaViolations;
using OmniRoute.Application.Features.Leads.Queries.GetTeamLeadOverview;
using OmniRoute.Application.Features.Leads.Queries.GetTeamLeads;
using OmniRoute.Application.Features.Teams.DTOs;
using OmniRoute.Application.Features.Teams.Queries.GetTeamReport;

namespace OmniRoute.API.Controllers;

[ApiController]
[Route("api/team-leads")]
[Authorize(Policy = "CanManageTeam")]
public sealed class TeamLeadsController : ControllerBase
{
    private readonly ISender _sender;

    public TeamLeadsController(ISender sender) => _sender = sender;

    /// <summary>TN-01 — Tổng quan queue và backlog của đội: PendingResponse, InProgress, SLA counts, trend 7 ngày.</summary>
    [HttpGet("overview")]
    [ProducesResponseType(typeof(TeamLeadOverviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetOverview(CancellationToken ct)
    {
        var result = await _sender.Send(new GetTeamLeadOverviewQuery(), ct);
        if (!result.IsSuccess)
            return BadRequest(new { result.ErrorCode, result.ErrorMessage });
        return Ok(result.Value);
    }

    /// <summary>TN-02 — Danh sách lead vi phạm SLA hoặc sắp vi phạm trong đội. Sắp xếp: vi phạm trước, gần deadline ASC.</summary>
    [HttpGet("sla-violations")]
    [ProducesResponseType(typeof(PagedResult<SlaViolationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetSlaViolations(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _sender.Send(new GetSlaViolationsQuery(page, pageSize), ct);
        if (!result.IsSuccess)
            return BadRequest(new { result.ErrorCode, result.ErrorMessage });
        return Ok(result.Value);
    }

    /// <summary>
    /// TN-03 — Tìm kiếm và lọc lead trong đội.
    /// Hỗ trợ lọc: search (tên/SĐT), status, priorityLevel, channel, assignedUserId, dateRange.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<TeamLeadListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetTeamLeads(
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
        var query = new GetTeamLeadsQuery(
            search, status, priorityLevel, channel, assignedUserId, dateFrom, dateTo, page, pageSize);
        var result = await _sender.Send(query, ct);
        if (!result.IsSuccess)
            return BadRequest(new { result.ErrorCode, result.ErrorMessage });
        return Ok(result.Value);
    }

    /// <summary>TN-04 — Reassign lead sang nhân viên khác trong đội.</summary>
    [HttpPatch("{leadId:guid}/reassign")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReassignLead(Guid leadId, [FromBody] ReassignLeadRequest request, CancellationToken ct)
    {
        var result = await _sender.Send(new ReassignLeadCommand(leadId, request.NewUserId, request.Reason), ct);
        if (!result.IsSuccess)
        {
            if (result.ErrorCode is "LEAD_NOT_FOUND" or "NEW_USER_NOT_FOUND")
                return NotFound(new { result.ErrorCode, result.ErrorMessage });
            return BadRequest(new { result.ErrorCode, result.ErrorMessage });
        }
        return NoContent();
    }

    /// <summary>TN-05 — Escalate lead ra ngoài đội (đến TN/QL/QT khác).</summary>
    [HttpPost("{leadId:guid}/escalate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> EscalateLead(Guid leadId, [FromBody] EscalateLeadRequest request, CancellationToken ct)
    {
        var result = await _sender.Send(new EscalateLeadCommand(leadId, request.EscalateTo, request.Reason), ct);
        if (!result.IsSuccess)
        {
            if (result.ErrorCode is "LEAD_NOT_FOUND" or "TARGET_NOT_FOUND")
                return NotFound(new { result.ErrorCode, result.ErrorMessage });
            return BadRequest(new { result.ErrorCode, result.ErrorMessage });
        }
        return NoContent();
    }

    /// <summary>TN-06 — Lịch sử escalate lead đã thực hiện bởi TN hiện tại.</summary>
    [HttpGet("escalate-history")]
    [ProducesResponseType(typeof(PagedResult<EscalateHistoryItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetEscalateHistory(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _sender.Send(new GetEscalateHistoryQuery(page, pageSize), ct);
        if (!result.IsSuccess)
            return BadRequest(new { result.ErrorCode, result.ErrorMessage });
        return Ok(result.Value);
    }

    /// <summary>TN-07 — Thêm ghi chú nội bộ trên lead (chỉ TN/QL/QT xem được).</summary>
    [HttpPost("{leadId:guid}/internal-notes")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddInternalNote(Guid leadId, [FromBody] AddInternalNoteRequest request, CancellationToken ct)
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

    /// <summary>TN-09 — Báo cáo tổng hợp hiệu suất đội theo kỳ.</summary>
    [HttpGet("report")]
    [ProducesResponseType(typeof(TeamReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetTeamReport(
        [FromQuery] string period = "month",
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null,
        CancellationToken ct = default)
    {
        var result = await _sender.Send(new GetTeamReportQuery(period, dateFrom, dateTo), ct);
        if (!result.IsSuccess)
            return BadRequest(new { result.ErrorCode, result.ErrorMessage });
        return Ok(result.Value);
    }
}

// Request bodies for PATCH/POST endpoints
public record ReassignLeadRequest(Guid NewUserId, string Reason);
public record EscalateLeadRequest(Guid EscalateTo, string Reason);
public record AddInternalNoteRequest(string Content);

