using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PRM.Application.Interfaces.Service;
using PRM.Infrastructure.AI.Provider;
using PRM.Infrastructure.Persistence;

namespace PRM.Infrastructure.AI
{
    /// <summary>
    /// Implements the Factory + Strategy pattern for LLM provider selection.
    ///
    /// FACTORY: Creates the correct ILlmClient based on DB config.
    /// STRATEGY: The chosen client encapsulates the provider-specific algorithm.
    ///
    /// Reading from DB on every call (instead of caching at startup) ensures
    /// that when the Admin changes the provider or API key, the change takes
    /// effect immediately without a server restart. This is YAGNI-safe — we
    /// add caching only if profiling shows DB reads here are a bottleneck.
    /// </summary>
    public class LlmClientFactory(
        PRMTDbContext context,
        IHttpClientFactory httpClientFactory,
        IOptions<LlmSettings> llmSettings) : ILlmClientFactory
    {
        public async Task<ILlmClient> GetClientAsync(CancellationToken ct = default)
        {
            var config = await context.SystemConfigurations.FirstOrDefaultAsync(ct)
                ?? throw new InvalidOperationException("System configuration not found. Run the database seeder.");

            var inHouseKey = !string.IsNullOrWhiteSpace(llmSettings.Value.InHouse.ApiKey)
                ? llmSettings.Value.InHouse.ApiKey
                : config.LlmApiKey;

            return config.LlmProvider.ToLowerInvariant() switch
            {
                "gemini" => new GeminiLlmClient(
                    httpClientFactory,
                    RequireApiKey(config.LlmApiKey, "Admin → System Configuration")),
                "groq" => new GroqLlmClient(
                    httpClientFactory,
                    RequireApiKey(config.LlmApiKey, "Admin → System Configuration")),
                "inhouse" => new InHouseLlmClient(
                    httpClientFactory,
                    RequireApiKey(
                        inHouseKey,
                        "LlmSettings:InHouse:ApiKey in appsettings.Development.json (local dev) " +
                        "or Admin → System Configuration"),
                    llmSettings.Value.InHouse),
                _ => throw new InvalidOperationException(
                    $"Unknown LLM provider '{config.LlmProvider}'. Supported: Gemini, Groq, InHouse.")
            };
        }

        private static string RequireApiKey(string? key, string sourceHint)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new InvalidOperationException(
                    $"LLM API key is not configured. Set it in {sourceHint}.");
            return key;
        }
    }
}
