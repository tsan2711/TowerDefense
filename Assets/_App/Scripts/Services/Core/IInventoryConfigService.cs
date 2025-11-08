using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Services.Data;

namespace Services.Core
{
    /// <summary>
    /// Service interface for managing Inventory Configuration
    /// Microservice: Handles configuration for inventory items (unlock costs, requirements, etc.)
    /// </summary>
    public interface IInventoryConfigService : IService
    {
        /// <summary>
        /// Event fired when inventory configuration is loaded
        /// </summary>
        event Action<List<InventoryConfigData>> OnInventoryConfigLoaded;

        /// <summary>
        /// Load all inventory configurations from Firestore
        /// </summary>
        Task<List<InventoryConfigData>> LoadInventoryConfigAsync();

        /// <summary>
        /// Load configuration for a specific tower
        /// </summary>
        /// <param name="towerName">Tower name</param>
        Task<InventoryConfigData> LoadTowerConfigAsync(string towerName);

        /// <summary>
        /// Get cached inventory configurations
        /// </summary>
        List<InventoryConfigData> GetCachedConfigs();

        /// <summary>
        /// Get configuration for a specific tower
        /// </summary>
        /// <param name="towerName">Tower name</param>
        InventoryConfigData GetTowerConfig(string towerName);

        /// <summary>
        /// Update configuration for a tower (admin function)
        /// </summary>
        /// <param name="config">Configuration data</param>
        Task<bool> UpdateTowerConfigAsync(InventoryConfigData config);

        /// <summary>
        /// Check if tower is unlockable (meets requirements)
        /// </summary>
        /// <param name="towerName">Tower name</param>
        /// <param name="userLevel">User level</param>
        /// <param name="userCurrency">User currency</param>
        bool CanUnlockTower(string towerName, int userLevel, int userCurrency);

        /// <summary>
        /// Get unlock cost for a tower
        /// </summary>
        /// <param name="towerName">Tower name</param>
        int GetUnlockCost(string towerName);

        /// <summary>
        /// Initialize inventory config collection with default data if empty
        /// </summary>
        Task<bool> InitializeCollectionIfEmptyAsync();
    }
}

