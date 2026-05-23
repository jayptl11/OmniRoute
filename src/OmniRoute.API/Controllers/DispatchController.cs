using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniRoute.Application.Common.Models;
using OmniRoute.Application.Features.Leads.Commands.DispatchLeadToStore;
using OmniRoute.Application.Features.Leads.DTOs;
using OmniRoute.Application.Features.Leads.Queries.GetDispatchHistory;
using OmniRoute.Application.Features.Leads.Queries.GetPendingDispatchLeadById;
using OmniRoute.Application.Features.Leads.Queries.GetPendingDispatchLeads;
using OmniRoute.Application.Features.Stores.DTOs;
using OmniRoute.Application.Features.Stores.Queries.GetStoresCapacity;

namespace OmniRoute.API.Controllers;

[ApiController]
[Route("api/dispatch")]
[Authorize(Policy = "CanDispatchToStore")]
public sealed class DispatchController : ControllerBase
{
    private readonly ISender _sender;

    public DispatchController(ISender sender) => _sender = sender;

    /// <summary>
    /// DP-01 + DP-07: Queue danh sách lead đang chờ chỉ định cửa hàng.
    /// Hỗ trợ tìm kiếm theo tên/SĐT, lọc theo priority, khu vực địa chỉ và thời gian chờ.
    /// Sắp xếp: PriorityLevel DESC → CreatedAt ASC (chờ lâu nhất lên trên).
    /// </summary>
    [HttpGet("queue")]
    [ProducesResponseType(typeof(GetPendingDispatchLeadsResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetQueue(
        [FromQuery] string? search,
        [FromQuery] string? priorityLevel,
        [FromQuery] string? addressContains,
        [FromQuery] int? waitedMoreThanMinutes,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var query = new GetPendingDispatchLeadsQuery(
            Search: search,
            PriorityLevel: priorityLevel,
            AddressContains: addressContains,
            WaitedMoreThanMinutes: waitedMoreThanMinutes,
            Page: page,
            PageSize: pageSize);

        var result = await _sender.Send(query, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { result.ErrorCode, result.ErrorMessage });
    }

    /// <summary>
    /// DP-02: Chi tiết một lead cụ thể đang chờ điều phối.
    /// Chỉ trả về lead có status = PendingDispatch.
    /// </summary>
    [HttpGet("queue/{id:guid}")]
    [ProducesResponseType(typeof(PendingDispatchLeadDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetQueueItem(Guid id, CancellationToken ct)
    {
        var result = await _sender.Send(new GetPendingDispatchLeadByIdQuery(id), ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { result.ErrorCode, result.ErrorMessage });
    }

    /// <summary>
    /// DP-03: Tình trạng từng cửa hàng — số lead đang active, slot còn trống.
    /// IsOverCapacity: cửa hàng đã vượt maxCapacity.
    /// IsNearCapacity: còn dưới 20% slot trống (cảnh báo gần đầy).
    /// </summary>
    [HttpGet("stores/capacity")]
    [ProducesResponseType(typeof(List<StoreCapacityDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStoresCapacity([FromQuery] string? q, CancellationToken ct)
    {
        var result = await _sender.Send(new GetStoresCapacityQuery(q), ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { result.ErrorCode, result.ErrorMessage });
    }

    /// <summary>
    /// DP-04 + DP-05: Gán lead về một cửa hàng cụ thể.
    /// Ghi note lý do chọn cửa hàng là tùy chọn (DP-05 tích hợp vào đây).
    /// Hệ thống tự tính SLA deadline theo StoreSupport × priority của lead.
    /// Store manager (QL) sẽ nhận notification sau khi gán thành công.
    /// </summary>
    [HttpPost("queue/{id:guid}/assign")]
    [ProducesResponseType(typeof(DispatchLeadToStoreResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssignToStore(
        Guid id,
        [FromBody] AssignToStoreRequest request,
        CancellationToken ct)
    {
        var command = new DispatchLeadToStoreCommand(
            LeadId: id,
            StoreId: request.StoreId,
            Note: request.Note);

        var result = await _sender.Send(command, ct);
        if (!result.IsSuccess)
        {
            if (result.ErrorCode is "NOT_FOUND" or "STORE_NOT_FOUND")
                return NotFound(new { result.ErrorCode, result.ErrorMessage });
            return BadRequest(new { result.ErrorCode, result.ErrorMessage });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// DP-06: Lịch sử phân công đã thực hiện bởi nhân viên DP hiện tại.
    /// Sắp xếp theo thời gian phân công giảm dần (gần nhất trước).
    /// </summary>
    [HttpGet("history")]
    [ProducesResponseType(typeof(List<DispatchHistoryItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHistory(CancellationToken ct)
    {
        var result = await _sender.Send(new GetDispatchHistoryQuery(), ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { result.ErrorCode, result.ErrorMessage });
    }
}

/// <summary>Request body cho DP-04: Gán lead về cửa hàng.</summary>
public record AssignToStoreRequest(
    Guid StoreId,
    string? Note
);
