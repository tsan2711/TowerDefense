using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Services.Data;

namespace Services.Core
{
    /// <summary>
    /// Service interface for managing Tower Level Data
    /// Microservice: Handles only Tower Data domain
    /// </summary>
    public interface ITowerDataService : IService
    {
        /// <summary>
        /// Event fired when TowerLevelData is loaded
        /// </summary>
        event Action<List<TowerLevelDataData>> OnTowerLevelDataLoaded;

        /// <summary>
        /// Load all TowerLevelData from Firestore
        /// </summary>
        Task<List<TowerLevelDataData>> LoadTowerLevelDataAsync();

        /// <summary>
        /// Load TowerLevelData filtered by type
        /// </summary>
        Task<List<TowerLevelDataData>> LoadTowerLevelDataByTypeAsync(int type);

        /// <summary>
        /// Get cached TowerLevelData
        /// </summary>
        List<TowerLevelDataData> GetCachedTowerLevelData();

        /// <summary>
        /// Initialize TowerLevelData collection with default data if empty
        /// </summary>
        Task<bool> InitializeCollectionIfEmptyAsync();
    }
}

