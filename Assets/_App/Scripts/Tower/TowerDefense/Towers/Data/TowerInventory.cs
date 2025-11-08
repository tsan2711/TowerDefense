using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TowerDefense.Towers.Data
{
    /// <summary>
    /// ScriptableObject that holds the player's selected towers (max 3)
    /// Used for gameplay to know which towers are available
    /// </summary>
    [CreateAssetMenu(fileName = "TowerInventory.asset", menuName = "TowerDefense/Tower Inventory", order = 2)]
    public class TowerInventory : ScriptableObject
    {
        [Header("Selected Towers")]
        [Tooltip("List of towers selected for gameplay (max 3)")]
        public List<Tower> selectedTowers = new List<Tower>();

        [Header("Tower Library Reference")]
        [Tooltip("Reference to the main tower library")]
        public TowerLibrary towerLibrary;

        /// <summary>
        /// Maximum number of towers that can be selected
        /// </summary>
        private const int MAX_SELECTED_TOWERS = 3;

        /// <summary>
        /// Get the maximum number of towers allowed
        /// </summary>
        public int MaxSelectedTowers => MAX_SELECTED_TOWERS;

        /// <summary>
        /// Get the count of currently selected towers
        /// </summary>
        public int SelectedCount => selectedTowers?.Count ?? 0;

        /// <summary>
        /// Check if inventory is full
        /// </summary>
        public bool IsFull => SelectedCount >= MAX_SELECTED_TOWERS;

        /// <summary>
        /// Check if can add more towers
        /// </summary>
        public bool CanAddMore => SelectedCount < MAX_SELECTED_TOWERS;

        /// <summary>
        /// Add a tower to selected towers
        /// </summary>
        /// <param name="tower">Tower to add</param>
        /// <returns>True if successfully added</returns>
        public bool AddTower(Tower tower)
        {
            if (tower == null)
            {
                Debug.LogWarning("[TowerInventory] Cannot add null tower");
                return false;
            }

            if (IsFull)
            {
                Debug.LogWarning($"[TowerInventory] Cannot add tower {tower.towerName} - inventory is full (max {MAX_SELECTED_TOWERS})");
                return false;
            }

            if (HasTower(tower))
            {
                Debug.LogWarning($"[TowerInventory] Tower {tower.towerName} is already selected");
                return false;
            }

            selectedTowers.Add(tower);
            Debug.Log($"[TowerInventory] Added tower {tower.towerName} to inventory ({SelectedCount}/{MAX_SELECTED_TOWERS})");
            return true;
        }

        /// <summary>
        /// Remove a tower from selected towers
        /// </summary>
        /// <param name="tower">Tower to remove</param>
        /// <returns>True if successfully removed</returns>
        public bool RemoveTower(Tower tower)
        {
            if (tower == null || selectedTowers == null)
            {
                return false;
            }

            bool removed = selectedTowers.Remove(tower);
            if (removed)
            {
                Debug.Log($"[TowerInventory] Removed tower {tower.towerName} from inventory ({SelectedCount}/{MAX_SELECTED_TOWERS})");
            }
            return removed;
        }

        /// <summary>
        /// Remove tower by name
        /// </summary>
        /// <param name="towerName">Name of the tower to remove</param>
        /// <returns>True if successfully removed</returns>
        public bool RemoveTowerByName(string towerName)
        {
            if (string.IsNullOrEmpty(towerName) || selectedTowers == null)
            {
                return false;
            }

            Tower tower = selectedTowers.FirstOrDefault(t => t.towerName == towerName);
            if (tower != null)
            {
                return RemoveTower(tower);
            }
            return false;
        }

        /// <summary>
        /// Clear all selected towers
        /// </summary>
        public void ClearAllTowers()
        {
            if (selectedTowers != null)
            {
                selectedTowers.Clear();
                Debug.Log("[TowerInventory] Cleared all selected towers");
            }
        }

        /// <summary>
        /// Check if a tower is already selected
        /// </summary>
        /// <param name="tower">Tower to check</param>
        /// <returns>True if tower is selected</returns>
        public bool HasTower(Tower tower)
        {
            return selectedTowers != null && selectedTowers.Contains(tower);
        }

        /// <summary>
        /// Check if a tower with this name is selected
        /// </summary>
        /// <param name="towerName">Tower name to check</param>
        /// <returns>True if tower is selected</returns>
        public bool HasTowerByName(string towerName)
        {
            return selectedTowers != null && selectedTowers.Any(t => t.towerName == towerName);
        }

        /// <summary>
        /// Get tower by name from selected towers
        /// </summary>
        /// <param name="towerName">Tower name</param>
        /// <returns>Tower if found, null otherwise</returns>
        public Tower GetTowerByName(string towerName)
        {
            return selectedTowers?.FirstOrDefault(t => t.towerName == towerName);
        }

        /// <summary>
        /// Set selected towers from tower names (loads from TowerLibrary)
        /// </summary>
        /// <param name="towerNames">List of tower names to select</param>
        /// <returns>True if successfully set</returns>
        public bool SetSelectedTowersFromNames(List<string> towerNames)
        {
            if (towerNames == null || towerNames.Count > MAX_SELECTED_TOWERS)
            {
                Debug.LogError($"[TowerInventory] Invalid tower names list (max {MAX_SELECTED_TOWERS})");
                return false;
            }

            if (towerLibrary == null)
            {
                Debug.LogError("[TowerInventory] Tower Library reference is missing!");
                return false;
            }

            ClearAllTowers();

            foreach (string towerName in towerNames)
            {
                if (string.IsNullOrEmpty(towerName))
                {
                    continue;
                }

                // Try to get tower from library
                if (towerLibrary.TryGetValue(towerName, out Tower tower))
                {
                    AddTower(tower);
                }
                else
                {
                    Debug.LogWarning($"[TowerInventory] Tower {towerName} not found in TowerLibrary");
                }
            }

            Debug.Log($"[TowerInventory] Set selected towers: {string.Join(", ", towerNames)}");
            return true;
        }

        /// <summary>
        /// Get list of selected tower names
        /// </summary>
        /// <returns>List of tower names</returns>
        public List<string> GetSelectedTowerNames()
        {
            if (selectedTowers == null)
            {
                return new List<string>();
            }

            return selectedTowers.Select(t => t.towerName).ToList();
        }

        /// <summary>
        /// Get tower by index
        /// </summary>
        /// <param name="index">Index of the tower</param>
        /// <returns>Tower at index, or null if out of range</returns>
        public Tower GetTowerAtIndex(int index)
        {
            if (selectedTowers == null || index < 0 || index >= selectedTowers.Count)
            {
                return null;
            }

            return selectedTowers[index];
        }

        /// <summary>
        /// Validate that selected towers don't exceed max limit
        /// Called in Unity Editor
        /// </summary>
        private void OnValidate()
        {
            if (selectedTowers != null && selectedTowers.Count > MAX_SELECTED_TOWERS)
            {
                Debug.LogWarning($"[TowerInventory] Too many towers selected! Limiting to {MAX_SELECTED_TOWERS}");
                selectedTowers = selectedTowers.Take(MAX_SELECTED_TOWERS).ToList();
            }
        }

        /// <summary>
        /// Sync selected towers from Services.Data.TowerInventoryData
        /// Used to sync with backend data
        /// </summary>
        /// <param name="inventoryData">Inventory data from backend</param>
        public void SyncWithInventoryData(Services.Data.TowerInventoryData inventoryData)
        {
            if (inventoryData == null)
            {
                Debug.LogWarning("[TowerInventory] Cannot sync with null inventory data");
                return;
            }

            List<string> selectedNames = inventoryData.GetSelectedTowerNames();
            SetSelectedTowersFromNames(selectedNames);
        }
    }
}

