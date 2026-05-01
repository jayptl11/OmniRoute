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
    /// Classifies the lead need using active API keys ordered by priority.
    /// Returns null only when all keys fail â€” caller should fall through to rule-based fallback.
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
                var cfg = ParseConfig(key.ConfigJson);
                var result = await CallProviderAsync(key.Provider, plainKey, cfg, needDescription, channel, ct);

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
        string configJson,
        string needDescription,
        string channel,
        CancellationToken ct = default)
    {
        var cfg = ParseConfig(configJson);
        return await CallProviderAsync(provider, plainKey, cfg, needDescription, channel, ct);
    }

    // ------------------------------------------------------------------
    // Config parsing
    // ------------------------------------------------------------------

    private sealed record ProviderConfig(string Model, double Temperature, int MaxTokens);

    private static ProviderConfig ParseConfig(string configJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(configJson);
            var root = doc.RootElement;

            var model = root.TryGetProperty("model", out var m) ? m.GetString() ?? "gpt-4o-mini" : "gpt-4o-mini";
            var temperature = root.TryGetProperty("temperature", out var t) ? t.GetDouble() : 0.0;
            var maxTokens = root.TryGetProperty("maxTokens", out var mt) ? mt.GetInt32() : 200;

            return new ProviderConfig(model, temperature, maxTokens);
        }
        catch
        {
            return new ProviderConfig("gpt-4o-mini", 0.0, 200);
        }
    }

    // ------------------------------------------------------------------
    // Provider dispatch
    // ------------------------------------------------------------------

    private Task<AiClassificationResult> CallProviderAsync(
        string provider,
        string plainKey,
        ProviderConfig cfg,
        string needDescription,
        string channel,
        CancellationToken ct)
    {
        return provider switch
        {
            "OpenAI"    => CallOpenAiCompatibleAsync("OpenAI",    "https://api.openai.com/v1/chat/completions",      plainKey, cfg, needDescription, channel, ct),
            "Groq"      => CallOpenAiCompatibleAsync("Groq",      "https://api.groq.com/openai/v1/chat/completions", plainKey, cfg, needDescription, channel, ct),
            "Gemini"    => CallGeminiAsync(plainKey, cfg, needDescription, channel, ct),
            "Anthropic" => CallAnthropicAsync(plainKey, cfg, needDescription, channel, ct),
            _           => throw new NotSupportedException($"AI provider '{provider}' is not supported.")
        };
    }

    // ------------------------------------------------------------------
    // OpenAI-compatible (OpenAI & Groq share the same Chat Completions format)
    // ------------------------------------------------------------------

    private async Task<AiClassificationResult> CallOpenAiCompatibleAsync(
        string providerName,
        string apiUrl,
        string apiKey,
        ProviderConfig cfg,
        string needDescription,
        string channel,
        CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient(providerName);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var prompt = BuildClassificationPrompt(needDescription, channel);
        var requestBody = new
        {
            model = cfg.Model,
            messages = new[]
            {
                new { role = "system", content = "You are a Vietnamese customer service routing assistant. Respond only with valid JSON." },
                new { role = "user", content = prompt }
            },
            temperature = cfg.Temperature,
            max_tokens = cfg.MaxTokens
        };

        var response = await PostJsonAsync(client, apiUrl, requestBody, ct);
        var content = response
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? string.Empty;

        return ParseClassificationResponse(content, providerName);
    }

    // ------------------------------------------------------------------
    // Gemini
    // ------------------------------------------------------------------

    private async Task<AiClassificationResult> CallGeminiAsync(
        string apiKey,
        ProviderConfig cfg,
        string needDescription,
        string channel,
        CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient("Gemini");
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{cfg.Model}:generateContent?key={apiKey}";

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
            generationConfig = new { temperature = cfg.Temperature, maxOutputTokens = cfg.MaxTokens }
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
        ProviderConfig cfg,
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
            model = cfg.Model,
            max_tokens = cfg.MaxTokens,
            temperature = cfg.Temperature,
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
            PhÃ¢n loáº¡i nhu cáº§u khÃ¡ch hÃ ng dá»±a trÃªn ná»™i dung sau.
            KÃªnh: {channel}
            Ná»™i dung: {needDescription}

            PhÃ¢n loáº¡i vÃ o má»™t trong cÃ¡c nhÃ³m sau:
            - SaleNew: Mua hÃ ng má»›i, há»i giÃ¡, Ä‘Äƒng kÃ½, láº¯p Ä‘áº·t
            - SaleUpgrade: NÃ¢ng cáº¥p gÃ³i/thiáº¿t bá»‹
            - SaleRenew: Gia háº¡n há»£p Ä‘á»“ng
            - CskhSupport: Há»— trá»£ sau bÃ¡n, hÆ°á»›ng dáº«n sá»­ dá»¥ng
            - CskhComplaint: Khiáº¿u náº¡i, phÃ n nÃ n dá»‹ch vá»¥
            - CskhWarranty: Báº£o hÃ nh, sá»­a chá»¯a
            - StoreVisit: YÃªu cáº§u Ä‘áº¿n cá»­a hÃ ng trá»±c tiáº¿p
            - Other: KhÃ´ng xÃ¡c Ä‘á»‹nh Ä‘Æ°á»£c

            Tráº£ lá»i Ä‘Ãºng Ä‘á»‹nh dáº¡ng JSON sau, khÃ´ng kÃ¨m text ngoÃ i:
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
