using System;
using System.Collections.Generic;
using System.Linq;

namespace Services.Data
{
    /// <summary>
    /// Serializable data model for user's tower inventory
    /// Contains all towers owned by the user and their selection state
    /// </summary>
    [Serializable]
    public class TowerInventoryData
    {
        /// <summary>
        /// User ID this inventory belongs to
        /// </summary>
        public string userId;

        /// <summary>
        /// List of all towers owned by the user
        /// </summary>
        public List<InventoryItemData> ownedTowers;

        /// <summary>
        /// Maximum number of towers that can be selected for gameplay
        /// </summary>
        public int maxSelectedTowers = 3;

        /// <summary>
        /// Last time the inventory was updated
        /// </summary>
        public long lastUpdated;

        public TowerInventoryData()
        {
            ownedTowers = new List<InventoryItemData>();
            maxSelectedTowers = 3;
            lastUpdated = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }

        public TowerInventoryData(string userId)
        {
            this.userId = userId;
            ownedTowers = new List<InventoryItemData>();
            maxSelectedTowers = 3;
            lastUpdated = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }

        /// <summary>
        /// Check if user owns a specific tower
        /// </summary>
        public bool HasTower(string towerName)
        {
            return ownedTowers != null && ownedTowers.Any(t => t.towerName == towerName);
        }

        /// <summary>
        /// Get a specific tower from inventory
        /// </summary>
        public InventoryItemData GetTower(string towerName)
        {
            return ownedTowers?.FirstOrDefault(t => t.towerName == towerName);
        }

        /// <summary>
        /// Add a tower to the inventory
        /// </summary>
        public bool AddTower(InventoryItemData tower)
        {
            if (ownedTowers == null)
            {
                ownedTowers = new List<InventoryItemData>();
            }

            if (HasTower(tower.towerName))
            {
                return false; // Already owned
            }

            ownedTowers.Add(tower);
            lastUpdated = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return true;
        }

        /// <summary>
        /// Remove a tower from the inventory
        /// </summary>
        public bool RemoveTower(string towerName)
        {
            if (ownedTowers == null)
            {
                return false;
            }

            var tower = GetTower(towerName);
            if (tower == null)
            {
                return false;
            }

            ownedTowers.Remove(tower);
            lastUpdated = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return true;
        }

        /// <summary>
        /// Get all currently selected towers
        /// </summary>
        public List<InventoryItemData> GetSelectedTowers()
        {
            return ownedTowers?.Where(t => t.isSelected).ToList() ?? new List<InventoryItemData>();
        }

        /// <summary>
        /// Get selected tower names
        /// </summary>
        public List<string> GetSelectedTowerNames()
        {
            return GetSelectedTowers().Select(t => t.towerName).ToList();
        }

        /// <summary>
        /// Set selected towers (max 3)
        /// </summary>
        public bool SetSelectedTowers(List<string> towerNames)
        {
            if (towerNames == null || towerNames.Count > maxSelectedTowers)
            {
                return false;
            }

            // Verify all towers are owned
            foreach (var name in towerNames)
            {
                if (!HasTower(name))
                {
                    return false;
                }
            }

            // Deselect all towers
            if (ownedTowers != null)
            {
                foreach (var tower in ownedTowers)
                {
                    tower.isSelected = false;
                }

                // Select specified towers
                foreach (var name in towerNames)
                {
                    var tower = GetTower(name);
                    if (tower != null)
                    {
                        tower.isSelected = true;
                    }
                }
            }

            lastUpdated = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return true;
        }

        /// <summary>
        /// Increment usage count for a tower
        /// </summary>
        public void IncrementUsage(string towerName)
        {
            var tower = GetTower(towerName);
            if (tower != null)
            {
                tower.usageCount++;
                lastUpdated = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            }
        }

        /// <summary>
        /// Get count of selected towers
        /// </summary>
        public int GetSelectedCount()
        {
            return GetSelectedTowers().Count;
        }

        /// <summary>
        /// Check if can select more towers
        /// </summary>
        public bool CanSelectMore()
        {
            return GetSelectedCount() < maxSelectedTowers;
        }
    }
}

