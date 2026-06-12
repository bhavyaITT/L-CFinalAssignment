namespace PRM.Infrastructure.AI;

public class LlmSettings
{
    public const string SectionName = "LlmSettings";

    public InHouseLlmSettings InHouse { get; set; } = new();
}

public class InHouseLlmSettings
{
    /// <summary>Full URL, e.g. http://host/api/generate or http://host/v1/chat/completions</summary>
    public string BaseUrl { get; set; } = string.Empty;

    public string Model { get; set; } = "default";

    /// <summary>OllamaGenerate | OllamaChat | OpenAiChat</summary>
    public string ApiStyle { get; set; } = "OllamaGenerate";

    /// <summary>Kong | Bearer | XApiKey | ApiKey | ApiKeyHeader | QueryParam</summary>
    public string AuthMode { get; set; } = "Kong";

    /// <summary>Custom header name when AuthMode is ApiKeyHeader (default: X-Api-Key)</summary>
    public string? ApiKeyHeaderName { get; set; }

    /// <summary>Optional. If set, overrides the API key from system configuration (DB).</summary>
    public string? ApiKey { get; set; }
}
