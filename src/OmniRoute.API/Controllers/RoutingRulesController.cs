using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniRoute.Application.Features.RoutingRules.Commands.CreateRoutingRule;
using OmniRoute.Application.Features.RoutingRules.Commands.ToggleRoutingRuleStatus;
using OmniRoute.Application.Features.RoutingRules.Commands.UpdateRoutingRule;
using OmniRoute.Application.Features.RoutingRules.DTOs;
using OmniRoute.Application.Features.RoutingRules.Queries.GetRoutingRules;
using OmniRoute.Application.Features.RoutingRules.Queries.TestRoutingRule;
using OmniRoute.Domain.Enums;

namespace OmniRoute.API.Controllers;

[ApiController]
[Route("api/routing-rules")]
[Authorize(Policy = "CanAdminSystem")]
public sealed class RoutingRulesController : ControllerBase
{
    private readonly ISender _sender;

    public RoutingRulesController(ISender sender) => _sender = sender;

    /// <summary>QT-07 — Xem danh sách rule hiện hành</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<RoutingRuleDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await _sender.Send(new GetRoutingRulesQuery(), ct);
        return Ok(result.Value);
    }

    /// <summary>QT-06 — Tạo rule phân luồng mới</summary>
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateRoutingRuleCommand command, CancellationToken ct)
    {
        var result = await _sender.Send(command, ct);
        if (!result.IsSuccess)
            return BadRequest(new { result.ErrorCode, result.ErrorMessage });

        return CreatedAtAction(nameof(GetAll), new { }, result.Value);
    }

    /// <summary>QT-06 — Cập nhật rule phân luồng</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRoutingRuleRequest request, CancellationToken ct)
    {
        var command = new UpdateRoutingRuleCommand(
            id,
            request.RuleName,
            request.Description,
            request.PriorityOrder,
            request.ConditionChannels,
            request.ConditionKeywords,
            request.ActionGroup,
            request.ActionTeamId);

        var result = await _sender.Send(command, ct);
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "NOT_FOUND")
                return NotFound(new { result.ErrorCode, result.ErrorMessage });
            return BadRequest(new { result.ErrorCode, result.ErrorMessage });
        }

        return NoContent();
    }

    /// <summary>QT-08 — Bật / tắt rule</summary>
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ToggleStatus(Guid id, [FromBody] ToggleRoutingRuleStatusRequest request, CancellationToken ct)
    {
        var result = await _sender.Send(new ToggleRoutingRuleStatusCommand(id, request.IsActive), ct);
        if (!result.IsSuccess)
            return NotFound(new { result.ErrorCode, result.ErrorMessage });

        return NoContent();
    }

    /// <summary>QT-06 — Test rule: kiểm tra rule nào sẽ được áp dụng</summary>
    [HttpPost("test")]
    [ProducesResponseType(typeof(TestRoutingRuleResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> TestRule([FromBody] TestRoutingRuleRequest request, CancellationToken ct)
    {
        var result = await _sender.Send(new TestRoutingRuleQuery(request.NeedDescription, request.Channel), ct);
        return Ok(result.Value);
    }
}

public record UpdateRoutingRuleRequest(
    string RuleName,
    string? Description,
    int PriorityOrder,
    List<string>? ConditionChannels,
    List<string>? ConditionKeywords,
    AssignedGroup ActionGroup,
    Guid? ActionTeamId);

public record ToggleRoutingRuleStatusRequest(bool IsActive);

public record TestRoutingRuleRequest(string? NeedDescription, string? Channel);
