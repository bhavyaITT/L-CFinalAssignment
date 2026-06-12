using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRM.Application.Interfaces.Service
{
    /// <summary>
    /// Abstraction over any LLM provider (Gemini, Groq, etc.).
    /// Application layer calls this — it never imports any vendor SDK.
    /// This is the Adapter pattern: each concrete implementation adapts
    /// a third-party API to this common interface.
    /// Swapping providers = swapping one class, zero Application changes.
    /// </summary>
    public interface ILlmClient
    {
        /// <summary>
        /// Sends a prompt and returns the model's plain-text response.
        /// </summary>
        Task<string> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken ct = default);
    }
}
