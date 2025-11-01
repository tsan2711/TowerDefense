namespace Services.Core
{
    /// <summary>
    /// Base interface for all services in the microservice architecture
    /// </summary>
    public interface IService
    {
        /// <summary>
        /// Initialize the service
        /// </summary>
        void Initialize();

        /// <summary>
        /// Check if the service is initialized
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// Cleanup and shutdown the service
        /// </summary>
        void Shutdown();
    }
}

