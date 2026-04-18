using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniRoute.Application.Features.Leads.Commands.CreateLead;
using OmniRoute.Application.Features.Leads.DTOs;

namespace OmniRoute.API.Controllers;

[ApiController]
[Route("api/[controller]")]
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
    [Authorize(Policy = "CanCreateLead")]
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

        return CreatedAtAction(nameof(Create), new { id = response.LeadId }, response);
    }
}
