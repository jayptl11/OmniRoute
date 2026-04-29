using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniRoute.Application.Features.Teams.Commands.AddTeamMember;
using OmniRoute.Application.Features.Teams.Commands.RemoveTeamMember;
using OmniRoute.Application.Features.Teams.DTOs;
using OmniRoute.Application.Features.Teams.Queries.GetMemberPerformance;
using OmniRoute.Application.Features.Teams.Queries.GetTeamMembers;
using OmniRoute.Application.Features.Teams.Queries.SearchAddableUsers;
using OmniRoute.Application.Features.Tickets.Commands.AddInternalNote;

namespace OmniRoute.API.Controllers;

[ApiController]
[Route("api/my-team")]
[Authorize(Policy = "CanManageTeam")]
public sealed class MyTeamController : ControllerBase
{
    private readonly ISender _sender;

    public MyTeamController(ISender sender) => _sender = sender;

    /// <summary>TN-11 helper — Tìm kiếm user để thêm vào đội (theo tên hoặc username).</summary>
    [HttpGet("members/search")]
    [ProducesResponseType(typeof(List<AddableUserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SearchAddableUsers([FromQuery] string? q, CancellationToken ct)
    {
        var result = await _sender.Send(new SearchAddableUsersQuery(q), ct);
        if (!result.IsSuccess)
            return BadRequest(new { result.ErrorCode, result.ErrorMessage });
        return Ok(result.Value);
    }

    /// <summary>TN-10 — Xem danh sách thành viên trong đội của TN hiện tại</summary>
    [HttpGet("members")]
    [ProducesResponseType(typeof(List<TeamMemberDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetMembers(CancellationToken ct)
    {
        var result = await _sender.Send(new GetTeamMembersQuery(), ct);
        if (!result.IsSuccess)
            return BadRequest(new { result.ErrorCode, result.ErrorMessage });
        return Ok(result.Value);
    }

    /// <summary>TN-11 — Thêm thành viên vào đội</summary>
    [HttpPost("members")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddMember([FromBody] AddTeamMemberCommand command, CancellationToken ct)
    {
        var result = await _sender.Send(command, ct);
        if (!result.IsSuccess)
        {
            if (result.ErrorCode is "USER_NOT_FOUND" or "NO_TEAM")
                return NotFound(new { result.ErrorCode, result.ErrorMessage });
            return BadRequest(new { result.ErrorCode, result.ErrorMessage });
        }
        return NoContent();
    }

    /// <summary>TN-12 — Xóa thành viên khỏi đội. Trả về 409 nếu còn lead chưa hoàn tất.</summary>
    [HttpDelete("members/{userId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RemoveMember(Guid userId, CancellationToken ct)
    {
        var result = await _sender.Send(new RemoveTeamMemberCommand(userId), ct);
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "ACTIVE_LEADS_WARNING")
                return Conflict(new { result.ErrorCode, result.ErrorMessage });
            if (result.ErrorCode is "USER_NOT_IN_TEAM" or "USER_NOT_FOUND" or "NO_TEAM")
                return NotFound(new { result.ErrorCode, result.ErrorMessage });
            return BadRequest(new { result.ErrorCode, result.ErrorMessage });
        }
        return NoContent();
    }

    /// <summary>TN-08 — Xem hiệu suất từng thành viên trong đội theo kỳ.</summary>
    [HttpGet("members/{userId:guid}/performance")]
    [ProducesResponseType(typeof(MemberPerformanceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMemberPerformance(
        Guid userId,
        [FromQuery] string period = "month",
        CancellationToken ct = default)
    {
        var result = await _sender.Send(new GetMemberPerformanceQuery(userId, period), ct);
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "MEMBER_NOT_FOUND")
                return NotFound(new { result.ErrorCode, result.ErrorMessage });
            return BadRequest(new { result.ErrorCode, result.ErrorMessage });
        }
        return Ok(result.Value);
    }

    /// <summary>TN-07 — Thêm ghi chú nội bộ trên ticket (chỉ TN/QL/QT xem được).</summary>
    [HttpPost("tickets/{ticketId:guid}/internal-notes")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddTicketInternalNote(Guid ticketId, [FromBody] AddTicketInternalNoteRequest request, CancellationToken ct)
    {
        var result = await _sender.Send(new AddInternalNoteToTicketCommand(ticketId, request.Content), ct);
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "TICKET_NOT_FOUND")
                return NotFound(new { result.ErrorCode, result.ErrorMessage });
            return BadRequest(new { result.ErrorCode, result.ErrorMessage });
        }
        return NoContent();
    }
}

public record AddTicketInternalNoteRequest(string Content);

