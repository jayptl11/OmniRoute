using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OmniRoute.Application.Features.AiApiKeys.Commands.AddAiApiKey;
using OmniRoute.Application.Features.AiApiKeys.Commands.TestAiApiKey;
using OmniRoute.Application.Features.AiApiKeys.Commands.ToggleAiApiKeyStatus;
using OmniRoute.Application.Features.AiApiKeys.Commands.UpdateAiApiKey;
using OmniRoute.Application.Features.AiApiKeys.Queries.GetAiApiKeys;

namespace OmniRoute.API.Controllers;

[ApiController]
[Route("api/ai-api-keys")]
[Authorize(Policy = "CanAdminSystem")]
public sealed class AiApiKeysController : ControllerBase
{
    private readonly ISender _sender;

    public AiApiKeysController(ISender sender) => _sender = sender;

    /// <summary>Get all AI API keys (masked values).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<AiApiKeyDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await _sender.Send(new GetAiApiKeysQuery(), ct);
        return Ok(result.Value);
    }

    /// <summary>Add a new AI API key.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Add([FromBody] AddAiApiKeyRequest request, CancellationToken ct)
    {
        var command = new AddAiApiKeyCommand(
            request.Provider,
            request.DisplayName,
            request.PlainKeyValue,
            request.IsActive,
            request.Priority,
            request.Config.GetRawText());

        var result = await _sender.Send(command, ct);
        if (result.IsFailure)
            return BadRequest(new { result.ErrorCode, result.ErrorMessage });

        return CreatedAtAction(nameof(GetAll), new { }, result.Value);
    }

    /// <summary>Update display name, key value (optional), priority, or config of an existing key.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAiApiKeyRequest request, CancellationToken ct)
    {
        var command = new UpdateAiApiKeyCommand(
            id,
            request.DisplayName,
            request.PlainKeyValue,
            request.Priority,
            request.Config.GetRawText());

        var result = await _sender.Send(command, ct);
        if (result.IsFailure)
            return result.ErrorCode == "NOT_FOUND" ? NotFound(result.ErrorMessage) : BadRequest(new { result.ErrorCode, result.ErrorMessage });

        return NoContent();
    }

    /// <summary>Toggle active/inactive status of an AI API key.</summary>
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ToggleStatus(Guid id, CancellationToken ct)
    {
        var result = await _sender.Send(new ToggleAiApiKeyStatusCommand(id), ct);
        if (result.IsFailure)
            return result.ErrorCode == "NOT_FOUND" ? NotFound(result.ErrorMessage) : BadRequest(new { result.ErrorCode, result.ErrorMessage });

        return NoContent();
    }

    /// <summary>Test a specific AI API key with a sample prompt.</summary>
    [HttpPost("{id:guid}/test")]
    [ProducesResponseType(typeof(TestAiApiKeyResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Test(Guid id, CancellationToken ct)
    {
        var result = await _sender.Send(new TestAiApiKeyCommand(id), ct);
        if (result.IsFailure)
            return result.ErrorCode == "NOT_FOUND" ? NotFound(result.ErrorMessage) : BadRequest(new { result.ErrorCode, result.ErrorMessage });

        return Ok(result.Value);
    }
}

/// <summary>Request body for POST /api/ai-api-keys</summary>
public record AddAiApiKeyRequest(
    string Provider,
    string DisplayName,
    string PlainKeyValue,
    bool IsActive,
    int Priority,
    JsonElement Config);

/// <summary>Request body for PUT /api/ai-api-keys/{id}</summary>
public record UpdateAiApiKeyRequest(
    string DisplayName,
    string? PlainKeyValue,
    int Priority,
    JsonElement Config);

