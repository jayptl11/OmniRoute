using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniRoute.Application.Features.Stores.Commands.CreateStore;
using OmniRoute.Application.Features.Stores.Commands.ToggleStoreStatus;
using OmniRoute.Application.Features.Stores.Commands.UpdateStore;
using OmniRoute.Application.Features.Stores.DTOs;
using OmniRoute.Application.Features.Stores.Queries.GetStoreById;
using OmniRoute.Application.Features.Stores.Queries.GetStores;

namespace OmniRoute.API.Controllers;

[ApiController]
[Route("api/stores")]
[Authorize(Policy = "CanAdminSystem")]
public sealed class StoresController : ControllerBase
{
    private readonly ISender _sender;

    public StoresController(ISender sender) => _sender = sender;

    /// <summary>QT-10 — Danh sách cửa hàng (có lọc)</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<StoreDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStores(
        [FromQuery] string? region,
        [FromQuery] bool? isActive,
        CancellationToken ct)
    {
        var result = await _sender.Send(new GetStoresQuery(region, isActive), ct);
        return Ok(result.Value);
    }

    /// <summary>QT-10 — Chi tiết cửa hàng</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(StoreDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _sender.Send(new GetStoreByIdQuery(id), ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { result.ErrorCode, result.ErrorMessage });
    }

    /// <summary>QT-10 — Tạo cửa hàng mới</summary>
    [HttpPost]
    [ProducesResponseType(typeof(StoreDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateStoreCommand command, CancellationToken ct)
    {
        var result = await _sender.Send(command, ct);
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "CODE_TAKEN")
                return Conflict(new { result.ErrorCode, result.ErrorMessage });
            return BadRequest(new { result.ErrorCode, result.ErrorMessage });
        }
        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
    }

    /// <summary>QT-10 — Cập nhật cửa hàng</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateStoreCommand command, CancellationToken ct)
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

    /// <summary>QT-10 — Kích hoạt / vô hiệu hóa cửa hàng</summary>
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ToggleStatus(Guid id, [FromBody] ToggleStoreStatusRequest request, CancellationToken ct)
    {
        var result = await _sender.Send(new ToggleStoreStatusCommand(id, request.IsActive), ct);
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "NOT_FOUND")
                return NotFound(new { result.ErrorCode, result.ErrorMessage });
            return BadRequest(new { result.ErrorCode, result.ErrorMessage });
        }
        return NoContent();
    }
}

public record ToggleStoreStatusRequest(bool IsActive);
