using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Services.Data;

namespace Services.Core
{
    /// <summary>
    /// Service interface for managing User Tower Inventory
    /// Microservice: Handles user's tower collection and selection (max 3 towers)
    /// </summary>
    public interface IInventoryService : IService
    {
        /// <summary>
        /// Event fired when user's inventory is loaded or updated
        /// </summary>
        event Action<TowerInventoryData> OnInventoryLoaded;

        /// <summary>
        /// Event fired when selected towers changed
        /// </summary>
        event Action<List<string>> OnSelectedTowersChanged;

        /// <summary>
        /// Load user's tower inventory from Firestore
        /// </summary>
        /// <param name="userId">User ID</param>
        Task<TowerInventoryData> LoadUserInventoryAsync(string userId);

        /// <summary>
        /// Get cached user inventory
        /// </summary>
        TowerInventoryData GetCachedInventory();

        /// <summary>
        /// Add a tower to user's inventory (unlock)
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="towerName">Tower name to add</param>
        Task<bool> UnlockTowerAsync(string userId, string towerName);

        /// <summary>
        /// Remove a tower from user's inventory
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="towerName">Tower name to remove</param>
        Task<bool> RemoveTowerAsync(string userId, string towerName);

        /// <summary>
        /// Select towers for gameplay (max 3)
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="selectedTowerNames">List of tower names (max 3)</param>
        Task<bool> SelectTowersAsync(string userId, List<string> selectedTowerNames);

        /// <summary>
        /// Get currently selected towers (max 3)
        /// </summary>
        List<string> GetSelectedTowers();

        /// <summary>
        /// Check if user owns a specific tower
        /// </summary>
        /// <param name="towerName">Tower name to check</param>
        bool HasTower(string towerName);

        /// <summary>
        /// Get all available towers in user's inventory
        /// </summary>
        List<string> GetAvailableTowers();

        /// <summary>
        /// Initialize user inventory with default towers if new user
        /// </summary>
        /// <param name="userId">User ID</param>
        Task<bool> InitializeUserInventoryAsync(string userId);
    }
}

