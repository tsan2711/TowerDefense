using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Services.Data;

namespace Services.Core
{
    /// <summary>
    /// Service interface for managing Agent Configuration data
    /// Microservice: Handles only Agent Configuration domain
    /// </summary>
    public interface IAgentConfigurationService : IService
    {
        /// <summary>
        /// Event fired when AgentConfiguration data is loaded
        /// </summary>
        event Action<List<AgentConfigurationData>> OnAgentConfigurationsLoaded;

        /// <summary>
        /// Load all AgentConfiguration data from Firestore
        /// </summary>
        Task<List<AgentConfigurationData>> LoadAgentConfigurationsAsync();

        /// <summary>
        /// Load AgentConfiguration data filtered by type
        /// </summary>
        Task<List<AgentConfigurationData>> LoadAgentConfigurationsByTypeAsync(int type);

        /// <summary>
        /// Get cached AgentConfiguration data
        /// </summary>
        List<AgentConfigurationData> GetCachedAgentConfigurations();

        /// <summary>
        /// Initialize AgentConfigurations collection with default data if empty
        /// </summary>
        Task<bool> InitializeCollectionIfEmptyAsync();
    }
}

