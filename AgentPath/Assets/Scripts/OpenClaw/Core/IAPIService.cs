using System.Net;

namespace CR.OpenClaw
{
    /// <summary>
    /// Interface for API services that can register their own endpoints
    /// </summary>
    public interface IAPIService
    {
        /// <summary>
        /// Register all endpoints for this service
        /// </summary>
        /// <param name="registry">The endpoint registry to register with</param>
        void RegisterEndpoints(IEndpointRegistry registry);
    }

    /// <summary>
    /// Interface for registering endpoints
    /// </summary>
    public interface IEndpointRegistry
    {
        /// <summary>
        /// Register a GET endpoint
        /// </summary>
        void RegisterGet(string path, System.Func<HttpListenerRequest, string> handler);

        /// <summary>
        /// Register a POST endpoint
        /// </summary>
        void RegisterPost(string path, System.Func<HttpListenerRequest, string> handler);
    }
}
