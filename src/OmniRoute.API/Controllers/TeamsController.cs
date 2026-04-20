using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniRoute.Application.Features.Teams.Commands.CreateTeam;
using OmniRoute.Application.Features.Teams.Commands.ToggleTeamStatus;
using OmniRoute.Application.Features.Teams.Commands.UpdateTeam;
using OmniRoute.Application.Features.Teams.DTOs;
using OmniRoute.Application.Features.Teams.Queries.GetTeamById;
using OmniRoute.Application.Features.Teams.Queries.GetTeams;
using OmniRoute.Domain.Enums;

namespace OmniRoute.API.Controllers;

[ApiController]
[Route("api/teams")]
[Authorize(Policy = "CanAdminSystem")]
public sealed class TeamsController : ControllerBase
{
    private readonly ISender _sender;

    public TeamsController(ISender sender) => _sender = sender;

    /// <summary>QT-10 — Danh sách nhóm xử lý (có lọc)</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<TeamDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTeams(
        [FromQuery] AssignedGroup? teamType,
        [FromQuery] Guid? storeId,
        [FromQuery] bool? isActive,
        CancellationToken ct)
    {
        var result = await _sender.Send(new GetTeamsQuery(teamType, storeId, isActive), ct);
        return Ok(result.Value);
    }

    /// <summary>QT-10 — Chi tiết nhóm xử lý</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TeamDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _sender.Send(new GetTeamByIdQuery(id), ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { result.ErrorCode, result.ErrorMessage });
    }

    /// <summary>QT-10 — Tạo nhóm xử lý mới</summary>
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateTeamCommand command, CancellationToken ct)
    {
        var result = await _sender.Send(command, ct);
        if (!result.IsSuccess)
            return BadRequest(new { result.ErrorCode, result.ErrorMessage });

        return CreatedAtAction(nameof(GetById), new { id = result.Value }, result.Value);
    }

    /// <summary>QT-10 — Cập nhật nhóm xử lý</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTeamCommand command, CancellationToken ct)
    {
        if (id != command.Id)
            return BadRequest(new { ErrorCode = "ID_MISMATCH", ErrorMessage = "Route id and body Id must match." });

        var result = await _sender.Send(command, ct);
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "NOT_FOUND")
                return NotFound(new { result.ErrorCode, result.ErrorMessage });
            return BadRequest(new { result.ErrorCode, result.ErrorMessage });
        }
        return NoContent();
    }

    /// <summary>QT-10 — Kích hoạt / vô hiệu hóa nhóm</summary>
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ToggleStatus(Guid id, [FromBody] ToggleTeamStatusRequest request, CancellationToken ct)
    {
        var result = await _sender.Send(new ToggleTeamStatusCommand(id, request.IsActive), ct);
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "NOT_FOUND")
                return NotFound(new { result.ErrorCode, result.ErrorMessage });
            return BadRequest(new { result.ErrorCode, result.ErrorMessage });
        }
        return NoContent();
    }
}

public record ToggleTeamStatusRequest(bool IsActive);
