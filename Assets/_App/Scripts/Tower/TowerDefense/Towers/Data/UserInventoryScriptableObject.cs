using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Services.Data;

namespace TowerDefense.Towers.Data
{
    /// <summary>
    /// ScriptableObject chứa user's inventory data (owned towers)
    /// Khác với TowerInventory chỉ chứa selected towers (max 3)
    /// ScriptableObject này chứa tất cả towers mà user sở hữu
    /// </summary>
    [CreateAssetMenu(fileName = "UserInventory.asset", menuName = "TowerDefense/User Inventory", order = 4)]
    public class UserInventoryScriptableObject : ScriptableObject
    {
        [Header("User Data")]
        [Tooltip("User ID this inventory belongs to")]
        public string userId;

        [Header("Owned Towers")]
        [Tooltip("List of all towers owned by the user")]
        public List<InventoryItemData> ownedTowers = new List<InventoryItemData>();

        [Header("Settings")]
        [Tooltip("Maximum number of towers that can be selected for gameplay")]
        public int maxSelectedTowers = 3;

        [Tooltip("Last time the inventory was updated (timestamp)")]
        public long lastUpdated;

        /// <summary>
        /// Get count of owned towers
        /// </summary>
        public int OwnedCount => ownedTowers?.Count ?? 0;

        /// <summary>
        /// Get count of selected towers
        /// </summary>
        public int SelectedCount => ownedTowers?.Count(t => t.isSelected) ?? 0;

        /// <summary>
        /// Check if user owns a specific tower
        /// </summary>
        public bool HasTower(string towerName)
        {
            return ownedTowers != null && ownedTowers.Any(t => t != null && t.towerName == towerName);
        }

        /// <summary>
        /// Get a specific tower from inventory
        /// </summary>
        public InventoryItemData GetTower(string towerName)
        {
            return ownedTowers?.FirstOrDefault(t => t != null && t.towerName == towerName);
        }

        /// <summary>
        /// Get all selected towers
        /// </summary>
        public List<InventoryItemData> GetSelectedTowers()
        {
            return ownedTowers?.Where(t => t != null && t.isSelected).ToList() ?? new List<InventoryItemData>();
        }

        /// <summary>
        /// Get selected tower names
        /// </summary>
        public List<string> GetSelectedTowerNames()
        {
            return GetSelectedTowers().Select(t => t.towerName).ToList();
        }

        /// <summary>
        /// Sync data from TowerInventoryData (from Firestore)
        /// </summary>
        public void SyncFromInventoryData(TowerInventoryData inventoryData)
        {
            if (inventoryData == null)
            {
                Debug.LogWarning("[UserInventoryScriptableObject] Cannot sync from null inventory data");
                return;
            }

            userId = inventoryData.userId;
            maxSelectedTowers = inventoryData.maxSelectedTowers;
            lastUpdated = inventoryData.lastUpdated;

            if (inventoryData.ownedTowers != null)
            {
                ownedTowers = new List<InventoryItemData>(inventoryData.ownedTowers);
            }
            else
            {
                ownedTowers = new List<InventoryItemData>();
            }

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif

            Debug.Log($"[UserInventoryScriptableObject] Synced: {ownedTowers.Count} towers owned, {SelectedCount} selected");
        }

        /// <summary>
        /// Convert to TowerInventoryData (for saving to Firestore)
        /// </summary>
        public TowerInventoryData ToInventoryData()
        {
            TowerInventoryData data = new TowerInventoryData(userId)
            {
                maxSelectedTowers = this.maxSelectedTowers,
                lastUpdated = this.lastUpdated
            };

            if (ownedTowers != null)
            {
                data.ownedTowers = new List<InventoryItemData>(ownedTowers);
            }

            return data;
        }
    }
}

