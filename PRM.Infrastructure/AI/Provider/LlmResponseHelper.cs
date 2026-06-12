using System.Text.Json;

namespace PRM.Infrastructure.AI.Provider;

internal static class LlmResponseHelper
{
    public static string ExtractOpenAiChatContent(string json)
    {
        using var doc = JsonDocument.Parse(json);

        return doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString()
            ?? throw new InvalidOperationException("LLM returned an empty response.");
    }

    public static string ExtractGeminiText(string json)
    {
        using var doc = JsonDocument.Parse(json);

        return doc.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString()
            ?? throw new InvalidOperationException("LLM returned an empty response.");
    }

    public static string SummarizeError(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return "No error details returned.";

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("error", out var error))
            {
                if (error.TryGetProperty("message", out var message))
                    return message.GetString() ?? json;

                return error.ToString();
            }

            if (root.TryGetProperty("message", out var msg))
                return msg.GetString() ?? json;
        }
        catch
        {
            // fall through
        }

        return json.Length > 300 ? json[..300] + "..." : json;
    }
}
