using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniRoute.Application.Features.MasterData.Commands.CreateMasterDataItem;
using OmniRoute.Application.Features.MasterData.Commands.ToggleMasterDataItemStatus;
using OmniRoute.Application.Features.MasterData.Commands.UpdateMasterDataItem;
using OmniRoute.Application.Features.MasterData.DTOs;
using OmniRoute.Application.Features.MasterData.Queries.GetEnumList;
using OmniRoute.Application.Features.MasterData.Queries.GetMasterDataItems;
using OmniRoute.Domain.Enums;

namespace OmniRoute.API.Controllers;

[ApiController]
[Route("api/master-data")]
[Authorize(Policy = "CanAdminSystem")]
public sealed class MasterDataController : ControllerBase
{
    private readonly ISender _sender;

    public MasterDataController(ISender sender) => _sender = sender;

    /// <summary>QT-09 — Danh sách mục danh mục theo category</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<MasterDataItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetItems(
        [FromQuery] MasterDataCategory category,
        [FromQuery] bool? isActive,
        CancellationToken ct)
    {
        var result = await _sender.Send(new GetMasterDataItemsQuery(category, isActive), ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { result.ErrorCode, result.ErrorMessage });
    }

    /// <summary>QT-09 — Danh sách giá trị enum hệ thống (Channel / NeedType / LeadStatus)</summary>
    [HttpGet("enums/{enumType}")]
    [ProducesResponseType(typeof(List<EnumListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetEnumList(string enumType, CancellationToken ct)
    {
        var result = await _sender.Send(new GetEnumListQuery(enumType), ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { result.ErrorCode, result.ErrorMessage });
    }

    /// <summary>QT-09 — Tạo mới mục danh mục</summary>
    [HttpPost]
    [ProducesResponseType(typeof(MasterDataItemDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateMasterDataItemCommand command, CancellationToken ct)
    {
        var result = await _sender.Send(command, ct);
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "CODE_TAKEN")
                return Conflict(new { result.ErrorCode, result.ErrorMessage });
            return BadRequest(new { result.ErrorCode, result.ErrorMessage });
        }
        return CreatedAtAction(nameof(GetItems), new { category = command.Category }, result.Value);
    }

    /// <summary>QT-09 — Cập nhật mục danh mục</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateMasterDataItemRequest request, CancellationToken ct)
    {
        var result = await _sender.Send(new UpdateMasterDataItemCommand(id, request.DisplayName, request.Description, request.SortOrder), ct);
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "NOT_FOUND")
                return NotFound(new { result.ErrorCode, result.ErrorMessage });
            return BadRequest(new { result.ErrorCode, result.ErrorMessage });
        }
        return NoContent();
    }

    /// <summary>QT-09 — Ẩn / hiện mục danh mục (IsActive toggle)</summary>
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ToggleStatus(Guid id, [FromBody] ToggleMasterDataStatusRequest request, CancellationToken ct)
    {
        var result = await _sender.Send(new ToggleMasterDataItemStatusCommand(id, request.IsActive), ct);
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "NOT_FOUND")
                return NotFound(new { result.ErrorCode, result.ErrorMessage });
            return BadRequest(new { result.ErrorCode, result.ErrorMessage });
        }
        return NoContent();
    }
}

public record UpdateMasterDataItemRequest(string DisplayName, string? Description, int SortOrder);
public record ToggleMasterDataStatusRequest(bool IsActive);
