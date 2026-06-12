using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRM.Application.Interfaces.Service
{

    /// <summary>
    /// Factory that returns the correct ILlmClient based on the system-configured provider.
    /// This is the Factory + Strategy pattern combination:
    /// - Factory: creates the right provider object
    /// - Strategy: allows runtime switching of the algorithm (LLM call implementation)
    ///
    /// The Admin changes the provider in System Configuration → the factory reads that
    /// setting on every request → no restart required.
    /// </summary>
    public interface ILlmClientFactory
    {
        Task<ILlmClient> GetClientAsync(CancellationToken ct = default);
    }
}
