using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniRoute.Application.Common.Models;
using OmniRoute.Application.Features.Tickets.Commands.AddTicketNote;
using OmniRoute.Application.Features.Tickets.Commands.EscalateTicket;
using OmniRoute.Application.Features.Tickets.Commands.RecordSatisfaction;
using OmniRoute.Application.Features.Tickets.Commands.UpdateTicketStatus;
using OmniRoute.Application.Features.Tickets.DTOs;
using OmniRoute.Application.Features.Tickets.Queries.GetAssignedTicketById;
using OmniRoute.Application.Features.Tickets.Queries.GetAssignedTickets;
using OmniRoute.Application.Features.Tickets.Queries.GetTicketPerformance;

namespace OmniRoute.API.Controllers;

[ApiController]
[Route("api/tickets")]
[Authorize(Policy = "CanProcessTicket")]
public sealed class TicketsController : ControllerBase
{
    private readonly ISender _sender;

    public TicketsController(ISender sender) => _sender = sender;

    /// <summary>
    /// CS-01 + CS-03: Danh sách ticket được gán cho nhân viên CS hiện tại.
    /// Hỗ trợ lọc theo trạng thái, mức ưu tiên, ngày gán và tìm kiếm theo tên/SĐT.
    /// Sắp xếp: PriorityLevel DESC → SlaDeadline ASC (sắp vi phạm SLA trước).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<TicketListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAssigned(
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] string? priorityLevel,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var query = new GetAssignedTicketsQuery(search, status, priorityLevel, dateFrom, dateTo, page, pageSize);
        var result = await _sender.Send(query, ct);
        return Ok(result.Value);
    }

    /// <summary>
    /// CS-02: Chi tiết một ticket được gán, bao gồm activity timeline và lịch sử ticket của KH.
    /// Chỉ trả về ticket đang được gán cho nhân viên CS hiện tại.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TicketDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _sender.Send(new GetAssignedTicketByIdQuery(id), ct);

        if (result.IsFailure)
            return NotFound(new { result.ErrorCode, result.ErrorMessage });

        return Ok(result.Value);
    }

    /// <summary>
    /// CS-04: Cập nhật trạng thái ticket.
    /// Luồng hợp lệ (BR-05):
    ///   New              → InProgress
    ///   InProgress       → WaitingCustomer | Escalated | Resolved
    ///   WaitingCustomer  → InProgress | Resolved
    ///   Escalated        → Resolved
    ///   Resolved         → Closed
    /// Note bắt buộc khi chuyển sang InProgress hoặc Resolved.
    /// </summary>
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(UpdateTicketStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(
        Guid id,
        [FromBody] UpdateTicketStatusCommand command,
        CancellationToken ct)
    {
        if (id != command.TicketId)
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
    /// CS-05: Ghi chú kết quả xử lý cho một ticket.
    /// Không giới hạn số lượng ghi chú — mỗi ghi chú tạo 1 activity log PROCESSING_NOTE.
    /// </summary>
    [HttpPost("{id:guid}/notes")]
    [ProducesResponseType(typeof(AddTicketNoteResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddNote(
        Guid id,
        [FromBody] AddTicketNoteCommand command,
        CancellationToken ct)
    {
        if (id != command.TicketId)
            return BadRequest(new { ErrorCode = "ID_MISMATCH", ErrorMessage = "ID trong URL và body không khớp." });

        var result = await _sender.Send(command, ct);

        if (result.IsFailure)
        {
            if (result.ErrorCode is "NOT_FOUND")
                return NotFound(new { result.ErrorCode, result.ErrorMessage });

            return BadRequest(new { result.ErrorCode, result.ErrorMessage });
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Value!.TicketId }, result.Value);
    }

    /// <summary>
    /// CS-06: Escalate ticket vượt thẩm quyền.
    /// Ticket chuyển sang trạng thái Escalated. Người nhận nhận được notification.
    /// </summary>
    [HttpPost("{id:guid}/escalate")]
    [ProducesResponseType(typeof(EscalateTicketResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Escalate(
        Guid id,
        [FromBody] EscalateTicketCommand command,
        CancellationToken ct)
    {
        if (id != command.TicketId)
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
    /// CS-07: Ghi nhận mức độ hài lòng của khách hàng (score 1–5).
    /// Chỉ cho phép khi ticket ở trạng thái Resolved hoặc Closed.
    /// </summary>
    [HttpPatch("{id:guid}/satisfaction")]
    [ProducesResponseType(typeof(RecordSatisfactionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RecordSatisfaction(
        Guid id,
        [FromBody] RecordSatisfactionCommand command,
        CancellationToken ct)
    {
        if (id != command.TicketId)
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
    /// CS-08: Hiệu suất cá nhân nhân viên CS theo kỳ.
    /// period: "week" | "month" | "quarter".
    /// </summary>
    [HttpGet("performance")]
    [ProducesResponseType(typeof(TicketPerformanceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetPerformance(
        [FromQuery] string period = "month",
        CancellationToken ct = default)
    {
        var result = await _sender.Send(new GetTicketPerformanceQuery(period), ct);

        if (result.IsFailure)
            return BadRequest(new { result.ErrorCode, result.ErrorMessage });

        return Ok(result.Value);
    }
}
