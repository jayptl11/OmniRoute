using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OmniRoute.Application.Common.Interfaces;
using OmniRoute.Domain.Enums;
using OmniRoute.Domain.Interfaces;
using OmniRoute.Infrastructure.Settings;

namespace OmniRoute.Infrastructure.Services;

internal sealed class AiClassificationService : IAiClassificationService
{
    private readonly IAiApiKeyRepository _repository;
    private readonly IAiKeyEncryptionService _encryption;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptions<AiSettings> _settings;
    private readonly ILogger<AiClassificationService> _logger;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public AiClassificationService(
        IAiApiKeyRepository repository,
        IAiKeyEncryptionService encryption,
        IHttpClientFactory httpClientFactory,
        IOptions<AiSettings> settings,
        ILogger<AiClassificationService> logger)
    {
        _repository = repository;
        _encryption = encryption;
        _httpClientFactory = httpClientFactory;
        _settings = settings;
        _logger = logger;
    }

    /// <summary>
    /// Classifies the lead need using active API keys ordered by priority (primary first, then fallback).
    /// Returns null only when all keys fail — caller should fall through to rule-based fallback.
    /// </summary>
    public async Task<AiClassificationResult?> ClassifyAsync(
        string needDescription,
        string channel,
        CancellationToken ct = default)
    {
        var keys = await _repository.GetActiveKeysOrderedByPriorityAsync(ct);
        if (keys.Count == 0)
        {
            _logger.LogWarning("No active AI API keys configured. Skipping AI classification.");
            return null;
        }

        foreach (var key in keys)
        {
            try
            {
                var plainKey = _encryption.Decrypt(key.EncryptedKey);
                var result = await CallProviderAsync(key.Provider, plainKey, needDescription, channel, ct);

                key.RecordSuccess();
                await _repository.UpdateAsync(key, ct);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "AI provider {Provider} key {KeyId} failed. Trying next key.",
                    key.Provider, key.Id);

                key.RecordFailure();
                await _repository.UpdateAsync(key, ct);
            }
        }

        _logger.LogError("All AI API keys failed for classification. Returning null.");
        return null;
    }

    /// <inheritdoc />
    public async Task<AiClassificationResult> ClassifyWithKeyAsync(
        string provider,
        string plainKey,
        string needDescription,
        string channel,
        CancellationToken ct = default)
    {
        return await CallProviderAsync(provider, plainKey, needDescription, channel, ct);
    }

    // ------------------------------------------------------------------
    // Provider dispatch
    // ------------------------------------------------------------------

    private Task<AiClassificationResult> CallProviderAsync(
        string provider,
        string plainKey,
        string needDescription,
        string channel,
        CancellationToken ct)
    {
        return provider switch
        {
            "OpenAI" => CallOpenAiAsync(plainKey, needDescription, channel, ct),
            "Gemini" => CallGeminiAsync(plainKey, needDescription, channel, ct),
            "Anthropic" => CallAnthropicAsync(plainKey, needDescription, channel, ct),
            _ => throw new NotSupportedException($"AI provider '{provider}' is not supported.")
        };
    }

    // ------------------------------------------------------------------
    // OpenAI
    // ------------------------------------------------------------------

    private async Task<AiClassificationResult> CallOpenAiAsync(
        string apiKey,
        string needDescription,
        string channel,
        CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient("OpenAI");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var prompt = BuildClassificationPrompt(needDescription, channel);
        var requestBody = new
        {
            model = "gpt-4o-mini",
            messages = new[]
            {
                new { role = "system", content = "You are a Vietnamese customer service routing assistant. Respond only with valid JSON." },
                new { role = "user", content = prompt }
            },
            temperature = 0,
            max_tokens = 200
        };

        var response = await PostJsonAsync(client, "https://api.openai.com/v1/chat/completions", requestBody, ct);
        var content = response
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? string.Empty;

        return ParseClassificationResponse(content, "OpenAI");
    }

    // ------------------------------------------------------------------
    // Gemini
    // ------------------------------------------------------------------

    private async Task<AiClassificationResult> CallGeminiAsync(
        string apiKey,
        string needDescription,
        string channel,
        CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient("Gemini");
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key={apiKey}";

        var prompt = BuildClassificationPrompt(needDescription, channel);
        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = "You are a Vietnamese customer service routing assistant. Respond only with valid JSON.\n\n" + prompt }
                    }
                }
            },
            generationConfig = new { temperature = 0, maxOutputTokens = 200 }
        };

        var response = await PostJsonAsync(client, url, requestBody, ct);
        var content = response
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString() ?? string.Empty;

        return ParseClassificationResponse(content, "Gemini");
    }

    // ------------------------------------------------------------------
    // Anthropic
    // ------------------------------------------------------------------

    private async Task<AiClassificationResult> CallAnthropicAsync(
        string apiKey,
        string needDescription,
        string channel,
        CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient("Anthropic");
        client.DefaultRequestHeaders.Add("x-api-key", apiKey);
        client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

        var prompt = BuildClassificationPrompt(needDescription, channel);
        var requestBody = new
        {
            model = "claude-3-haiku-20240307",
            max_tokens = 200,
            messages = new[]
            {
                new { role = "user", content = "You are a Vietnamese customer service routing assistant. Respond only with valid JSON.\n\n" + prompt }
            }
        };

        var response = await PostJsonAsync(client, "https://api.anthropic.com/v1/messages", requestBody, ct);
        var content = response
            .GetProperty("content")[0]
            .GetProperty("text")
            .GetString() ?? string.Empty;

        return ParseClassificationResponse(content, "Anthropic");
    }

    // ------------------------------------------------------------------
    // Shared helpers
    // ------------------------------------------------------------------

    private static string BuildClassificationPrompt(string needDescription, string channel)
    {
        var jsonExample = """{"needType":"<value>","confidence":<0.0-1.0>,"reasoning":"<brief reason in Vietnamese>"}""";
        return $"""
            Phân loại nhu cầu khách hàng dựa trên nội dung sau.
            Kênh: {channel}
            Nội dung: {needDescription}

            Phân loại vào một trong các nhóm sau:
            - SaleNew: Mua hàng mới, hỏi giá, đăng ký, lắp đặt
            - SaleUpgrade: Nâng cấp gói/thiết bị
            - SaleRenew: Gia hạn hợp đồng
            - CskhSupport: Hỗ trợ sau bán, hướng dẫn sử dụng
            - CskhComplaint: Khiếu nại, phàn nàn dịch vụ
            - CskhWarranty: Bảo hành, sửa chữa
            - StoreVisit: Yêu cầu đến cửa hàng trực tiếp
            - Other: Không xác định được

            Trả lời đúng định dạng JSON sau, không kèm text ngoài:
            {jsonExample}
            """;
    }

    private static async Task<JsonElement> PostJsonAsync(
        HttpClient client,
        string url,
        object requestBody,
        CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(requestBody);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var response = await client.PostAsync(url, content, ct);

        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<JsonElement>(responseJson, JsonOpts);
    }

    private static AiClassificationResult ParseClassificationResponse(string raw, string provider)
    {
        // Strip potential markdown fences
        var json = raw.Trim();
        if (json.StartsWith("```"))
        {
            var start = json.IndexOf('{');
            var end = json.LastIndexOf('}');
            if (start >= 0 && end > start)
                json = json[start..(end + 1)];
        }

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var needTypeStr = root.GetProperty("needType").GetString() ?? "Other";
        var confidence = root.GetProperty("confidence").GetDouble();
        var reasoning = root.TryGetProperty("reasoning", out var r) ? r.GetString() ?? string.Empty : string.Empty;

        var needType = Enum.TryParse<NeedType>(needTypeStr, ignoreCase: true, out var parsed)
            ? parsed
            : NeedType.Other;

        return new AiClassificationResult(needType, confidence, reasoning, provider);
    }
}
