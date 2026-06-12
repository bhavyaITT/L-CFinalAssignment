using PRM.Application.Interfaces.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace PRM.Infrastructure.AI.Provider
{
    /// <summary>
    /// Adapter for Groq (uses OpenAI-compatible Chat Completions API).
    /// Same ILlmClient interface — zero changes needed in Application layer.
    /// </summary>
    public class GroqLlmClient(IHttpClientFactory httpClientFactory, string apiKey) : ILlmClient
    {
        private const string BaseUrl = "https://api.groq.com/openai/v1/chat/completions";
        private const string Model = "llama-3.3-70b-versatile";

        public async Task<string> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken ct = default)
        {
            var client = httpClientFactory.CreateClient("LlmClient");
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", apiKey);

            var body = new
            {
                model = Model,
                messages = new[]
                {
                new { role = "system", content = systemPrompt },
                new { role = "user",   content = userPrompt }
            },
                temperature = 0.3,
                max_tokens = 1024
            };

            var response = await client.PostAsJsonAsync(BaseUrl, body, ct);
            var json = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException(
                    $"Groq call failed ({(int)response.StatusCode}): {LlmResponseHelper.SummarizeError(json)}");

            return LlmResponseHelper.ExtractOpenAiChatContent(json);
        }
    }
}
