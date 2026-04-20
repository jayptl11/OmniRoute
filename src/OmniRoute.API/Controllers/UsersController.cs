using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniRoute.Application.Common.Models;
using OmniRoute.Application.Features.Users.Commands.AdminSendResetLink;
using OmniRoute.Application.Features.Users.Commands.AdminSetTemporaryPassword;
using OmniRoute.Application.Features.Users.Commands.CreateUser;
using OmniRoute.Application.Features.Users.Commands.ToggleUserStatus;
using OmniRoute.Application.Features.Users.Commands.UpdateUser;
using OmniRoute.Application.Features.Users.DTOs;
using OmniRoute.Application.Features.Users.Queries.GetUsers;

namespace OmniRoute.API.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Policy = "CanAdminSystem")]
public sealed class UsersController : ControllerBase
{
    private readonly ISender _sender;

    public UsersController(ISender sender) => _sender = sender;

    /// <summary>QT-01 — Danh sách tài khoản người dùng (có lọc và phân trang)</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<UserListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsers(
        [FromQuery] string? roleName,
        [FromQuery] Guid? storeId,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var query = new GetUsersQuery(roleName, storeId, isActive, page, pageSize);
        var result = await _sender.Send(query, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.ErrorMessage);
    }

    /// <summary>QT-02 — Tạo tài khoản người dùng mới</summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreateUserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateUser(
        [FromBody] CreateUserCommand command,
        CancellationToken ct)
    {
        var result = await _sender.Send(command, ct);
        if (!result.IsSuccess)
        {
            if (result.ErrorCode is "EMAIL_TAKEN" or "USERNAME_TAKEN")
                return Conflict(new { result.ErrorCode, result.ErrorMessage });
            return BadRequest(new { result.ErrorCode, result.ErrorMessage });
        }
        return CreatedAtAction(nameof(GetUsers), new { }, result.Value);
    }

    /// <summary>QT-03 — Chỉnh sửa thông tin tài khoản</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateUser(
        Guid id,
        [FromBody] UpdateUserCommand command,
        CancellationToken ct)
    {
        if (id != command.UserId)
            return BadRequest(new { ErrorCode = "ID_MISMATCH", ErrorMessage = "Route id and body UserId must match." });

        var result = await _sender.Send(command, ct);
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "USER_NOT_FOUND")
                return NotFound(new { result.ErrorCode, result.ErrorMessage });
            return BadRequest(new { result.ErrorCode, result.ErrorMessage });
        }
        return NoContent();
    }

    /// <summary>QT-04 — Khóa / mở khóa tài khoản</summary>
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(ToggleUserStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ToggleStatus(
        Guid id,
        [FromBody] ToggleStatusRequest request,
        CancellationToken ct)
    {
        var result = await _sender.Send(new ToggleUserStatusCommand(id, request.IsActive), ct);
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "USER_NOT_FOUND")
                return NotFound(new { result.ErrorCode, result.ErrorMessage });
            return BadRequest(new { result.ErrorCode, result.ErrorMessage });
        }
        return Ok(result.Value);
    }

    /// <summary>QT-05 — Gửi link đặt lại mật khẩu cho người dùng qua email</summary>
    [HttpPost("{id:guid}/send-reset-link")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SendResetLink(Guid id, CancellationToken ct)
    {
        var result = await _sender.Send(new AdminSendResetLinkCommand(id), ct);
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "USER_NOT_FOUND")
                return NotFound(new { result.ErrorCode, result.ErrorMessage });
            return BadRequest(new { result.ErrorCode, result.ErrorMessage });
        }
        return NoContent();
    }

    /// <summary>QT-05 — Đặt mật khẩu tạm thời cho người dùng (buộc đổi lần đăng nhập tiếp)</summary>
    [HttpPost("{id:guid}/set-temporary-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetTemporaryPassword(
        Guid id,
        [FromBody] SetTemporaryPasswordRequest request,
        CancellationToken ct)
    {
        var result = await _sender.Send(new AdminSetTemporaryPasswordCommand(id, request.TemporaryPassword), ct);
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "USER_NOT_FOUND")
                return NotFound(new { result.ErrorCode, result.ErrorMessage });
            return BadRequest(new { result.ErrorCode, result.ErrorMessage });
        }
        return NoContent();
    }
}

public record ToggleStatusRequest(bool IsActive);
public record SetTemporaryPasswordRequest(string TemporaryPassword);
