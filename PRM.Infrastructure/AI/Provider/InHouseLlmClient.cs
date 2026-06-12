using PRM.Application.Interfaces.Service;
using PRM.Infrastructure.AI;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace PRM.Infrastructure.AI.Provider;

/// <summary>
/// Adapter for an in-house LLM (Ollama /api/generate, Ollama /api/chat, or OpenAI-compatible).
/// API key from system configuration; endpoint and auth style from appsettings.
/// </summary>
public class InHouseLlmClient(
    IHttpClientFactory httpClientFactory,
    string apiKey,
    InHouseLlmSettings settings) : ILlmClient
{
    public async Task<string> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(settings.BaseUrl))
            throw new InvalidOperationException(
                "In-house LLM BaseUrl is not configured. Set LlmSettings:InHouse:BaseUrl in appsettings.json.");

        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException(
                "LLM API key is not configured. For local dev, set LlmSettings:InHouse:ApiKey " +
                "in appsettings.Development.json. Otherwise use Admin → System Configuration.");

        var client = httpClientFactory.CreateClient("LlmClient");
        var url = BuildUrl(settings.BaseUrl.Trim(), apiKey, settings);
        var body = BuildRequestBody(systemPrompt, userPrompt, settings);

        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(body)
        };

        ApplyAuth(request, apiKey, settings);

        var response = await client.SendAsync(request, ct);
        var json = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(
                $"In-house LLM call failed ({(int)response.StatusCode}): {LlmResponseHelper.SummarizeError(json)}");

        return ExtractResponse(json, settings.ApiStyle);
    }

    private static string BuildUrl(string baseUrl, string apiKey, InHouseLlmSettings settings)
    {
        if (string.Equals(settings.AuthMode, "QueryParam", StringComparison.OrdinalIgnoreCase))
            return $"{baseUrl}?api_key={Uri.EscapeDataString(apiKey)}";

        return baseUrl;
    }

    private static object BuildRequestBody(
        string systemPrompt, string userPrompt, InHouseLlmSettings settings)
    {
        var style = settings.ApiStyle.ToLowerInvariant();

        if (style == "openaichat")
        {
            return new
            {
                model = settings.Model,
                messages = new object[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
                },
                temperature = 0.3,
                max_tokens = 1024
            };
        }

        if (style == "ollamachat")
        {
            return new
            {
                model = settings.Model,
                messages = new object[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
                },
                stream = false,
                options = new { temperature = 0.3 }
            };
        }

        var combinedPrompt = $"""
            {systemPrompt}

            ---

            {userPrompt}
            """;

        return new
        {
            model = settings.Model,
            prompt = combinedPrompt,
            stream = false,
            options = new { temperature = 0.3 }
        };
    }

    private static void ApplyAuth(HttpRequestMessage request, string apiKey, InHouseLlmSettings settings)
    {
        switch (settings.AuthMode.ToLowerInvariant())
        {
            case "bearer":
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                break;

            case "authorizationapikey":
                request.Headers.Authorization = new AuthenticationHeaderValue("ApiKey", apiKey);
                break;

            case "apikeyheader":
                var headerName = string.IsNullOrWhiteSpace(settings.ApiKeyHeaderName)
                    ? "X-Api-Key"
                    : settings.ApiKeyHeaderName;
                request.Headers.TryAddWithoutValidation(headerName, apiKey);
                break;

            case "api-key":
            case "apikey":
                request.Headers.TryAddWithoutValidation("api-key", apiKey);
                break;

            case "xapikey":
                request.Headers.TryAddWithoutValidation("X-Api-Key", apiKey);
                break;

            case "queryparam":
                break;

            // Kong / Higress key-auth default header name
            case "kong":
            default:
                request.Headers.TryAddWithoutValidation("apikey", apiKey);
                break;
        }
    }

    private static string ExtractResponse(string json, string apiStyle)
    {
        var style = apiStyle.ToLowerInvariant();

        if (style == "openaichat")
            return LlmResponseHelper.ExtractOpenAiChatContent(json);

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (style == "ollamachat" && root.TryGetProperty("message", out var message)
            && message.TryGetProperty("content", out var chatContent))
        {
            return chatContent.GetString()
                ?? throw new InvalidOperationException("In-house LLM returned an empty chat response.");
        }

        if (root.TryGetProperty("response", out var ollamaResponse))
        {
            return ollamaResponse.GetString()
                ?? throw new InvalidOperationException("In-house LLM returned an empty response.");
        }

        try { return LlmResponseHelper.ExtractOpenAiChatContent(json); }
        catch
        {
            if (root.TryGetProperty("text", out var text))
                return text.GetString() ?? throw new InvalidOperationException("Empty LLM response.");
            throw new InvalidOperationException("Unrecognised in-house LLM response format.");
        }
    }
}
