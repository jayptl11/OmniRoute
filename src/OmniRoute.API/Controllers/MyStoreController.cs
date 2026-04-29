using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniRoute.Application.Features.StoreManagement.Commands.AssignStoreStaff;
using OmniRoute.Application.Features.StoreManagement.Commands.UnassignStoreStaff;
using OmniRoute.Application.Features.StoreManagement.DTOs;
using OmniRoute.Application.Features.StoreManagement.Queries.GetStoreCapacity;
using OmniRoute.Application.Features.StoreManagement.Queries.GetStoreStaff;
using OmniRoute.Application.Features.StoreManagement.Queries.GetStoreStaffWorkload;
using OmniRoute.Application.Features.StoreManagement.Queries.SearchAddableStoreUsers;

namespace OmniRoute.API.Controllers;

[ApiController]
[Route("api/my-store")]
[Authorize(Policy = "CanManageStore")]
public sealed class MyStoreController : ControllerBase
{
    private readonly ISender _sender;

    public MyStoreController(ISender sender) => _sender = sender;

    /// <summary>QL-07 helper — Tìm kiếm user để thêm vào đơn vị (theo tên hoặc username).</summary>
    [HttpGet("members/search")]
    [ProducesResponseType(typeof(List<AddableStoreUserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SearchAddableUsers([FromQuery] string? q, CancellationToken ct)
    {
        var result = await _sender.Send(new SearchAddableStoreUsersQuery(q), ct);
        if (!result.IsSuccess)
            return BadRequest(new { result.ErrorCode, result.ErrorMessage });
        return Ok(result.Value);
    }

    /// <summary>QL-06 — Xem danh sách nhân sự trong đơn vị của QL hiện tại.</summary>
    [HttpGet("members")]
    [ProducesResponseType(typeof(List<StoreStaffDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetMembers(CancellationToken ct)
    {
        var result = await _sender.Send(new GetStoreStaffQuery(), ct);
        if (!result.IsSuccess)
            return BadRequest(new { result.ErrorCode, result.ErrorMessage });
        return Ok(result.Value);
    }

    /// <summary>QL-07 — Thêm nhân sự vào đơn vị.</summary>
    [HttpPost("members")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddMember([FromBody] AssignStoreStaffCommand command, CancellationToken ct)
    {
        var result = await _sender.Send(command, ct);
        if (!result.IsSuccess)
        {
            if (result.ErrorCode is "USER_NOT_FOUND" or "NO_STORE")
                return NotFound(new { result.ErrorCode, result.ErrorMessage });
            return BadRequest(new { result.ErrorCode, result.ErrorMessage });
        }
        return NoContent();
    }

    /// <summary>QL-08 — Xóa / chuyển nhân sự khỏi đơn vị. Trả về 409 nếu còn lead chưa hoàn tất.</summary>
    [HttpDelete("members/{userId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RemoveMember(Guid userId, CancellationToken ct)
    {
        var result = await _sender.Send(new UnassignStoreStaffCommand(userId), ct);
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "ACTIVE_LEADS_WARNING")
                return Conflict(new { result.ErrorCode, result.ErrorMessage });
            if (result.ErrorCode is "USER_NOT_IN_STORE" or "USER_NOT_FOUND" or "NO_STORE")
                return NotFound(new { result.ErrorCode, result.ErrorMessage });
            return BadRequest(new { result.ErrorCode, result.ErrorMessage });
        }
        return NoContent();
    }

    /// <summary>QL-02 — Theo dõi tải và tiến độ của từng nhân sự trong đơn vị.</summary>
    [HttpGet("workload")]
    [ProducesResponseType(typeof(List<StoreStaffWorkloadDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetWorkload(CancellationToken ct)
    {
        var result = await _sender.Send(new GetStoreStaffWorkloadQuery(), ct);
        if (!result.IsSuccess)
            return BadRequest(new { result.ErrorCode, result.ErrorMessage });
        return Ok(result.Value);
    }

    /// <summary>QL-09 — Xem năng lực tiếp nhận của cửa hàng (max_capacity, active leads, available slots).</summary>
    [HttpGet("capacity")]
    [ProducesResponseType(typeof(StoreCapacityResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCapacity(CancellationToken ct)
    {
        var result = await _sender.Send(new GetStoreCapacityQuery(), ct);
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "STORE_NOT_FOUND")
                return NotFound(new { result.ErrorCode, result.ErrorMessage });
            return BadRequest(new { result.ErrorCode, result.ErrorMessage });
        }
        return Ok(result.Value);
    }
}
