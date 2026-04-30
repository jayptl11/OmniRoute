using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniRoute.Application.Features.Notifications.Commands.MarkAllAsRead;
using OmniRoute.Application.Features.Notifications.Commands.MarkAsRead;
using OmniRoute.Application.Features.Notifications.DTOs;
using OmniRoute.Application.Features.Notifications.Queries.GetMyNotifications;
using OmniRoute.Application.Features.Notifications.Queries.GetUnreadCount;

namespace OmniRoute.API.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public sealed class NotificationsController : ControllerBase
{
    private readonly ISender _sender;

    public NotificationsController(ISender sender) => _sender = sender;

    /// <summary>GET /api/notifications — get current user's notifications (paginated, newest first)</summary>
    [HttpGet]
    [ProducesResponseType(typeof(GetNotificationsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyNotifications(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _sender.Send(new GetMyNotificationsQuery(page, pageSize), ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.ErrorMessage);
    }

    /// <summary>GET /api/notifications/unread-count — number of unread notifications for current user</summary>
    [HttpGet("unread-count")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUnreadCount(CancellationToken ct)
    {
        var result = await _sender.Send(new GetUnreadNotificationCountQuery(), ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.ErrorMessage);
    }

    /// <summary>PUT /api/notifications/{id}/read — mark a single notification as read</summary>
    [HttpPut("{id:guid}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken ct)
    {
        var result = await _sender.Send(new MarkNotificationAsReadCommand(id), ct);
        if (!result.IsSuccess)
        {
            return result.ErrorCode switch
            {
                "NOT_FOUND" => NotFound(result.ErrorMessage),
                "FORBIDDEN" => Forbid(),
                _ => BadRequest(result.ErrorMessage)
            };
        }
        return NoContent();
    }

    /// <summary>PUT /api/notifications/read-all — mark all notifications as read for current user</summary>
    [HttpPut("read-all")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken ct)
    {
        var result = await _sender.Send(new MarkAllNotificationsAsReadCommand(), ct);
        return result.IsSuccess ? NoContent() : BadRequest(result.ErrorMessage);
    }
}
