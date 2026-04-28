using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniRoute.Application.Common.Models;
using OmniRoute.Application.Features.Leads.Commands.AddLeadNote;
using OmniRoute.Application.Features.Leads.Commands.CreateFollowUpTask;
using OmniRoute.Application.Features.Leads.Commands.ReportInvalidLead;
using OmniRoute.Application.Features.Leads.Commands.UpdateLeadStatus;
using OmniRoute.Application.Features.Leads.DTOs;
using OmniRoute.Application.Features.Leads.Queries.GetAssignedLeadById;
using OmniRoute.Application.Features.Leads.Queries.GetAssignedLeads;
using OmniRoute.Application.Features.Leads.Queries.GetFollowUpTasks;
using OmniRoute.Application.Features.Leads.Queries.GetPersonalPerformance;

namespace OmniRoute.API.Controllers;

[ApiController]
[Route("api/sale-leads")]
[Authorize(Policy = "CanProcessLead")]
public sealed class SaleLeadsController : ControllerBase
{
    private readonly ISender _sender;

    public SaleLeadsController(ISender sender) => _sender = sender;

    /// <summary>
    /// SA-01 + SA-03: Danh sách lead được gán cho nhân viên Sale hiện tại.
    /// Hỗ trợ lọc theo trạng thái, mức ưu tiên, kênh, ngày gán và tìm kiếm theo tên/SĐT.
    /// Sắp xếp: PriorityLevel DESC → SlaDeadline ASC (sắp vi phạm SLA trước).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<SaleLeadListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAssigned(
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] string? priorityLevel,
        [FromQuery] string? channel,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var query = new GetAssignedLeadsQuery(search, status, priorityLevel, channel, dateFrom, dateTo, page, pageSize);
        var result = await _sender.Send(query, ct);
        return Ok(result.Value);
    }

    /// <summary>
    /// SA-02: Chi tiết một lead được gán, bao gồm toàn bộ activity timeline.
    /// Chỉ trả về lead đang được gán cho nhân viên Sale hiện tại.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(SaleLeadDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _sender.Send(new GetAssignedLeadByIdQuery(id), ct);

        if (result.IsFailure)
            return NotFound(new { result.ErrorCode, result.ErrorMessage });

        return Ok(result.Value);
    }

    /// <summary>
    /// SA-04: Cập nhật trạng thái xử lý của lead.
    /// Chỉ được phép chuyển theo luồng (BR-05):
    ///   Assigned → Contacted | Cancelled
    ///   Contacted → InProgress | Cancelled
    ///   InProgress → Won | Lost | Cancelled
    /// Note bắt buộc khi Contacted/InProgress; LostReason khi Lost; CancelReason khi Cancelled.
    /// </summary>
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(UpdateLeadStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(
        Guid id,
        [FromBody] UpdateLeadStatusCommand command,
        CancellationToken ct)
    {
        if (id != command.LeadId)
            return BadRequest(new { ErrorCode = "ID_MISMATCH", ErrorMessage = "ID trong URL và body không khớp." });

        var result = await _sender.Send(command, ct);

        if (result.IsFailure)
        {
            if (result.ErrorCode is "NOT_FOUND")
                return NotFound(new { result.ErrorCode, result.ErrorMessage });

            return BadRequest(new { result.ErrorCode, result.ErrorMessage });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// SA-05: Ghi chú nội dung tư vấn cho một lead.
    /// Không giới hạn số lượng ghi chú — mỗi ghi chú tạo 1 activity log.
    /// </summary>
    [HttpPost("{id:guid}/notes")]
    [ProducesResponseType(typeof(AddLeadNoteResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddNote(
        Guid id,
        [FromBody] AddLeadNoteCommand command,
        CancellationToken ct)
    {
        if (id != command.LeadId)
            return BadRequest(new { ErrorCode = "ID_MISMATCH", ErrorMessage = "ID trong URL và body không khớp." });

        var result = await _sender.Send(command, ct);

        if (result.IsFailure)
        {
            if (result.ErrorCode is "NOT_FOUND")
                return NotFound(new { result.ErrorCode, result.ErrorMessage });

            return BadRequest(new { result.ErrorCode, result.ErrorMessage });
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Value!.LeadId }, result.Value);
    }

    /// <summary>
    /// SA-08: Báo lead không hợp lệ (Spam / SĐT sai / Không liên lạc được).
    /// Chuyển lead sang Cancelled và gửi notification đến Trưởng nhóm để review.
    /// </summary>
    [HttpPatch("{id:guid}/report-invalid")]
    [ProducesResponseType(typeof(ReportInvalidLeadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReportInvalid(
        Guid id,
        [FromBody] ReportInvalidLeadCommand command,
        CancellationToken ct)
    {
        if (id != command.LeadId)
            return BadRequest(new { ErrorCode = "ID_MISMATCH", ErrorMessage = "ID trong URL và body không khớp." });

        var result = await _sender.Send(command, ct);

        if (result.IsFailure)
        {
            if (result.ErrorCode is "NOT_FOUND")
                return NotFound(new { result.ErrorCode, result.ErrorMessage });

            return BadRequest(new { result.ErrorCode, result.ErrorMessage });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// SA-06: Đặt nhắc nhở follow-up cho một lead.
    /// DueAt phải ở trong tương lai (UTC).
    /// </summary>
    [HttpPost("{id:guid}/follow-ups")]
    [ProducesResponseType(typeof(CreateFollowUpTaskResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateFollowUp(
        Guid id,
        [FromBody] CreateFollowUpTaskCommand command,
        CancellationToken ct)
    {
        if (id != command.LeadId)
            return BadRequest(new { ErrorCode = "ID_MISMATCH", ErrorMessage = "ID trong URL và body không khớp." });

        var result = await _sender.Send(command, ct);

        if (result.IsFailure)
        {
            if (result.ErrorCode is "NOT_FOUND")
                return NotFound(new { result.ErrorCode, result.ErrorMessage });

            return BadRequest(new { result.ErrorCode, result.ErrorMessage });
        }

        return StatusCode(StatusCodes.Status201Created, result.Value);
    }

    /// <summary>
    /// SA-07: Danh sách nhắc nhở follow-up chưa hoàn thành của nhân viên SA hiện tại.
    /// filter: null = tất cả | "today" = hôm nay | "upcoming" = sắp đến | "overdue" = đã quá hạn.
    /// Sắp xếp theo DueAt ASC.
    /// </summary>
    [HttpGet("follow-ups")]
    [ProducesResponseType(typeof(List<FollowUpTaskListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFollowUps(
        [FromQuery] string? filter,
        CancellationToken ct)
    {
        var result = await _sender.Send(new GetFollowUpTasksQuery(filter), ct);
        return Ok(result.Value);
    }

    /// <summary>
    /// SA-09: Hiệu suất cá nhân theo kỳ.
    /// period: "week" | "month" | "quarter".
    /// </summary>
    [HttpGet("performance")]
    [ProducesResponseType(typeof(PersonalPerformanceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetPerformance(
        [FromQuery] string period = "month",
        CancellationToken ct = default)
    {
        var result = await _sender.Send(new GetPersonalPerformanceQuery(period), ct);

        if (result.IsFailure)
            return BadRequest(new { result.ErrorCode, result.ErrorMessage });

        return Ok(result.Value);
    }
}
