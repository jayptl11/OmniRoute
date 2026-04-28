using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniRoute.Application.Common.Models;
using OmniRoute.Application.Features.Leads.Commands.AddLeadNote;
using OmniRoute.Application.Features.Leads.Commands.UpdateLeadStatus;
using OmniRoute.Application.Features.Leads.DTOs;
using OmniRoute.Application.Features.Leads.Queries.GetAssignedLeadById;
using OmniRoute.Application.Features.Leads.Queries.GetAssignedLeads;

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
}
