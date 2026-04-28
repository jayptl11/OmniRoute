using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniRoute.Application.Features.Leads.Commands.CreateLead;
using OmniRoute.Application.Features.Leads.Commands.UpdateLead;
using OmniRoute.Application.Features.Leads.DTOs;
using OmniRoute.Application.Features.Leads.Queries.CheckDuplicate;
using OmniRoute.Application.Features.Leads.Queries.GetLeadById;
using OmniRoute.Application.Features.Leads.Queries.GetLeads;
using OmniRoute.Application.Common.Models;

namespace OmniRoute.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "CanCreateLead")]
public sealed class LeadsController : ControllerBase
{
    private readonly ISender _sender;

    public LeadsController(ISender sender) => _sender = sender;

    /// <summary>
    /// TV-01: Tạo lead / yêu cầu mới.
    /// Trả về 200 + isDuplicate=true nếu trùng SĐT và ForceCreate=false.
    /// Trả về 201 khi tạo mới thành công.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreateLeadResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(CreateLeadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] CreateLeadCommand command, CancellationToken ct)
    {
        var result = await _sender.Send(command, ct);

        if (result.IsFailure)
            return BadRequest(new { result.ErrorCode, result.ErrorMessage });

        var response = result.Value!;

        // Duplicate detected — let caller decide
        if (response.IsDuplicate)
            return Ok(response);

        return CreatedAtAction(nameof(GetById), new { id = response.LeadId }, response);
    }

    /// <summary>
    /// TV-02: Kiểm tra duplicate realtime theo SĐT.
    /// Gọi khi user blur khỏi field SĐT (debounce 500ms phía FE).
    /// </summary>
    [HttpGet("check-duplicate")]
    [ProducesResponseType(typeof(DuplicateCheckDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CheckDuplicate([FromQuery] string phone, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return BadRequest(new { ErrorCode = "INVALID_PHONE", ErrorMessage = "Số điện thoại không được để trống." });

        var result = await _sender.Send(new CheckDuplicateQuery(phone), ct);
        return Ok(result.Value);
    }

    /// <summary>
    /// TV-05 + TV-07: Danh sách lead đã tạo + tìm kiếm.
    /// Chỉ trả về lead do chính người dùng hiện tại tạo.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<LeadListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] string? channel,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var query = new GetLeadsQuery(search, status, channel, dateFrom, dateTo, page, pageSize);
        var result = await _sender.Send(query, ct);
        return Ok(result.Value);
    }

    /// <summary>
    /// TV-06 + TV-03: Chi tiết một lead, bao gồm kết quả phân loại tự động (NeedType, PriorityScore, AssignedGroup).
    /// Chỉ trả về lead do chính người dùng hiện tại tạo.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(LeadDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _sender.Send(new GetLeadByIdQuery(id), ct);

        if (result.IsFailure)
            return NotFound(new { result.ErrorCode, result.ErrorMessage });

        return Ok(result.Value);
    }

    /// <summary>
    /// TV-04: Chỉnh sửa thông tin bổ sung của lead.
    /// Chỉ TV có thể sửa lead do mình tạo và chưa ở trạng thái kết thúc.
    /// Không cho phép sửa: CustomerPhone, Channel, kết quả phân loại.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(UpdateLeadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateLeadCommand command, CancellationToken ct)
    {
        if (id != command.LeadId)
            return BadRequest(new { ErrorCode = "ID_MISMATCH", ErrorMessage = "ID trong URL và body không khớp." });

        var result = await _sender.Send(command, ct);

        if (result.IsFailure)
        {
            if (result.ErrorCode == "NOT_FOUND")
                return NotFound(new { result.ErrorCode, result.ErrorMessage });

            return BadRequest(new { result.ErrorCode, result.ErrorMessage });
        }

        return Ok(result.Value);
    }
}
