using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniRoute.Application.Features.NotificationConfigs.Commands.UpdateNotificationConfig;
using OmniRoute.Application.Features.NotificationConfigs.DTOs;
using OmniRoute.Application.Features.NotificationConfigs.Queries.GetNotificationConfigs;

namespace OmniRoute.API.Controllers;

/// <summary>QT-12: Notification/alert configuration (QT admin only)</summary>
[ApiController]
[Route("api/notification-configs")]
[Authorize(Policy = "CanAdminSystem")]
public sealed class NotificationConfigsController : ControllerBase
{
    private readonly ISender _sender;

    public NotificationConfigsController(ISender sender) => _sender = sender;

    /// <summary>GET /api/notification-configs — list all notification type→role mappings</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<NotificationConfigDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await _sender.Send(new GetNotificationConfigsQuery(), ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.ErrorMessage);
    }

    /// <summary>PUT /api/notification-configs/{id} — enable or disable a specific config entry</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateNotificationConfigRequest request, CancellationToken ct)
    {
        var result = await _sender.Send(new UpdateNotificationConfigCommand(id, request.IsEnabled), ct);
        if (!result.IsSuccess)
            return result.ErrorCode == "NOT_FOUND" ? NotFound(result.ErrorMessage) : BadRequest(result.ErrorMessage);
        return NoContent();
    }
}

public record UpdateNotificationConfigRequest(bool IsEnabled);
