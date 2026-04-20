using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniRoute.Application.Features.SlaConfig.Commands.ToggleSlaConfigStatus;
using OmniRoute.Application.Features.SlaConfig.Commands.UpdateSlaConfig;
using OmniRoute.Application.Features.SlaConfig.DTOs;
using OmniRoute.Application.Features.SlaConfig.Queries.GetSlaConfigs;

namespace OmniRoute.API.Controllers;

[ApiController]
[Route("api/sla-config")]
[Authorize(Policy = "CanAdminSystem")]
public sealed class SlaConfigController : ControllerBase
{
    private readonly ISender _sender;

    public SlaConfigController(ISender sender) => _sender = sender;

    /// <summary>QT-11 — Danh sách cấu hình SLA</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<SlaConfigDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await _sender.Send(new GetSlaConfigsQuery(), ct);
        return Ok(result.Value);
    }

    /// <summary>QT-11 — Cập nhật cấu hình SLA</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSlaConfigRequest request, CancellationToken ct)
    {
        var result = await _sender.Send(new UpdateSlaConfigCommand(id, request.MaxHours, request.WarningBeforeHours), ct);
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "NOT_FOUND")
                return NotFound(new { result.ErrorCode, result.ErrorMessage });
            return BadRequest(new { result.ErrorCode, result.ErrorMessage });
        }
        return NoContent();
    }

    /// <summary>QT-11 — Kích hoạt / vô hiệu hóa cấu hình SLA</summary>
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ToggleStatus(Guid id, [FromBody] ToggleSlaConfigStatusRequest request, CancellationToken ct)
    {
        var result = await _sender.Send(new ToggleSlaConfigStatusCommand(id, request.IsActive), ct);
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "NOT_FOUND")
                return NotFound(new { result.ErrorCode, result.ErrorMessage });
            return BadRequest(new { result.ErrorCode, result.ErrorMessage });
        }
        return NoContent();
    }
}

public record UpdateSlaConfigRequest(int MaxHours, int WarningBeforeHours);
public record ToggleSlaConfigStatusRequest(bool IsActive);
