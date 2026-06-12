using PRM.Application.Interfaces.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace PRM.Infrastructure.AI.Provider
{
    /// <summary>
    /// Adapter for Google Gemini.
    /// Adapts the Gemini HTTP API to our ILlmClient interface.
    /// The Application layer never sees any Gemini-specific code.
    /// </summary>
    public class GeminiLlmClient(IHttpClientFactory httpClientFactory, string apiKey) : ILlmClient
    {
        private const string BaseUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent";

        public async Task<string> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken ct = default)
        {
            var client = httpClientFactory.CreateClient("LlmClient");

            var url = $"{BaseUrl}?key={apiKey}";

            var body = new
            {
                system_instruction = new
                {
                    parts = new[] { new { text = systemPrompt } }
                },
                contents = new[]
                {
                new
                {
                    role = "user",
                    parts = new[] { new { text = userPrompt } }
                }
            },
                generationConfig = new
                {
                    temperature = 0.3,    // Low temperature = more factual, less creative
                    maxOutputTokens = 1024
                }
            };

            var response = await client.PostAsJsonAsync(url, body, ct);
            var json = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException(
                    $"Gemini call failed ({(int)response.StatusCode}): {LlmResponseHelper.SummarizeError(json)}");

            return LlmResponseHelper.ExtractGeminiText(json);
        }
    }
}
