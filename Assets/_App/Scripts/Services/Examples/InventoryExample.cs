using System.Collections.Generic;
using UnityEngine;
using Services.Core;
using Services.Data;
using Services.Managers;
using TowerDefense.Towers.Data;

namespace Services.Examples
{
    /// <summary>
    /// Example script demonstrating how to use the Inventory System
    /// This shows common use cases for managing tower inventory
    /// </summary>
    public class InventoryExample : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("TowerInventory ScriptableObject for gameplay")]
        public TowerInventory towerInventory;

        [Header("Test Data")]
        [Tooltip("User ID for testing")]
        public string testUserId = "testUser123";

        [Tooltip("Tower name to unlock")]
        public string towerToUnlock = "Laser1";

        private IInventoryService inventoryService;
        private IInventoryConfigService configService;

        void Start()
        {
            // Get services from ServiceLocator
            inventoryService = ServiceLocator.Instance.GetService<IInventoryService>();
            configService = ServiceLocator.Instance.GetService<IInventoryConfigService>();

            if (inventoryService == null || configService == null)
            {
                Debug.LogError("[InventoryExample] Services not found! Make sure they are registered in ServiceLocator.");
                return;
            }

            // Subscribe to events
            inventoryService.OnInventoryLoaded += OnInventoryLoaded;
            inventoryService.OnSelectedTowersChanged += OnSelectedTowersChanged;
            configService.OnInventoryConfigLoaded += OnConfigLoaded;

            Debug.Log("[InventoryExample] Initialized. Use the buttons in Inspector or call methods from other scripts.");
        }

        void OnDestroy()
        {
            // Unsubscribe from events
            if (inventoryService != null)
            {
                inventoryService.OnInventoryLoaded -= OnInventoryLoaded;
                inventoryService.OnSelectedTowersChanged -= OnSelectedTowersChanged;
            }

            if (configService != null)
            {
                configService.OnInventoryConfigLoaded -= OnConfigLoaded;
            }
        }

        #region Event Handlers

        private void OnInventoryLoaded(TowerInventoryData inventory)
        {
            Debug.Log($"[InventoryExample] Inventory loaded: {inventory.ownedTowers.Count} towers owned");
            
            // Sync with TowerInventory ScriptableObject for gameplay
            if (towerInventory != null)
            {
                towerInventory.SyncWithInventoryData(inventory);
                Debug.Log($"[InventoryExample] Synced {towerInventory.SelectedCount} towers to ScriptableObject");
            }
        }

        private void OnSelectedTowersChanged(List<string> selectedTowers)
        {
            Debug.Log($"[InventoryExample] Selection changed: {string.Join(", ", selectedTowers)}");
        }

        private void OnConfigLoaded(List<InventoryConfigData> configs)
        {
            Debug.Log($"[InventoryExample] Config loaded: {configs.Count} tower configurations");
        }

        #endregion

        #region Public Methods (Can be called from UI buttons or other scripts)

        /// <summary>
        /// Load user's inventory from backend
        /// </summary>
        public async void LoadInventory()
        {
            if (inventoryService == null)
            {
                Debug.LogError("[InventoryExample] Inventory service not available");
                return;
            }

            Debug.Log($"[InventoryExample] Loading inventory for user {testUserId}...");
            TowerInventoryData inventory = await inventoryService.LoadUserInventoryAsync(testUserId);

            if (inventory != null)
            {
                Debug.Log($"[InventoryExample] ✅ Loaded successfully!");
                DisplayInventoryInfo(inventory);
            }
            else
            {
                Debug.LogError("[InventoryExample] ❌ Failed to load inventory");
            }
        }

        /// <summary>
        /// Load all tower configurations
        /// </summary>
        public async void LoadConfigurations()
        {
            if (configService == null)
            {
                Debug.LogError("[InventoryExample] Config service not available");
                return;
            }

            Debug.Log("[InventoryExample] Loading tower configurations...");
            List<InventoryConfigData> configs = await configService.LoadInventoryConfigAsync();

            if (configs != null && configs.Count > 0)
            {
                Debug.Log($"[InventoryExample] ✅ Loaded {configs.Count} configurations");
                DisplayConfigInfo(configs);
            }
            else
            {
                Debug.LogError("[InventoryExample] ❌ Failed to load configurations");
            }
        }

        /// <summary>
        /// Unlock a tower for the user
        /// </summary>
        public async void UnlockTower()
        {
            if (inventoryService == null || configService == null)
            {
                Debug.LogError("[InventoryExample] Services not available");
                return;
            }

            // Check if tower config exists
            InventoryConfigData config = configService.GetTowerConfig(towerToUnlock);
            if (config == null)
            {
                Debug.LogError($"[InventoryExample] Tower config not found: {towerToUnlock}");
                return;
            }

            Debug.Log($"[InventoryExample] Attempting to unlock {towerToUnlock}...");
            Debug.Log($"  - Cost: {config.unlockCost}");
            Debug.Log($"  - Required Level: {config.requiredLevel}");

            bool success = await inventoryService.UnlockTowerAsync(testUserId, towerToUnlock);

            if (success)
            {
                Debug.Log($"[InventoryExample] ✅ Successfully unlocked {towerToUnlock}!");
            }
            else
            {
                Debug.LogError($"[InventoryExample] ❌ Failed to unlock {towerToUnlock}");
            }
        }

        /// <summary>
        /// Select first 3 owned towers
        /// </summary>
        public async void SelectFirstThreeTowers()
        {
            if (inventoryService == null)
            {
                Debug.LogError("[InventoryExample] Inventory service not available");
                return;
            }

            TowerInventoryData inventory = inventoryService.GetCachedInventory();
            if (inventory == null || inventory.ownedTowers.Count == 0)
            {
                Debug.LogWarning("[InventoryExample] No towers owned. Load inventory first.");
                return;
            }

            // Get first 3 towers
            List<string> towersToSelect = new List<string>();
            int count = Mathf.Min(3, inventory.ownedTowers.Count);
            
            for (int i = 0; i < count; i++)
            {
                towersToSelect.Add(inventory.ownedTowers[i].towerName);
            }

            Debug.Log($"[InventoryExample] Selecting towers: {string.Join(", ", towersToSelect)}");
            bool success = await inventoryService.SelectTowersAsync(testUserId, towersToSelect);

            if (success)
            {
                Debug.Log("[InventoryExample] ✅ Towers selected successfully!");
            }
            else
            {
                Debug.LogError("[InventoryExample] ❌ Failed to select towers");
            }
        }

        /// <summary>
        /// Check if user can unlock a specific tower
        /// </summary>
        public void CheckUnlockRequirements()
        {
            if (configService == null)
            {
                Debug.LogError("[InventoryExample] Config service not available");
                return;
            }

            InventoryConfigData config = configService.GetTowerConfig(towerToUnlock);
            if (config == null)
            {
                Debug.LogError($"[InventoryExample] Tower config not found: {towerToUnlock}");
                return;
            }

            // Mock user data for testing
            int userLevel = 10;
            int userCurrency = 5000;
            HashSet<string> completedLevels = new HashSet<string> { "level_1", "level_2", "level_3" };

            bool canUnlock = config.CanUnlock(userLevel, userCurrency, completedLevels);
            string statusMessage = config.GetUnlockStatusMessage(userLevel, userCurrency, completedLevels);

            Debug.Log($"[InventoryExample] Check unlock for {towerToUnlock}:");
            Debug.Log($"  - Can Unlock: {canUnlock}");
            Debug.Log($"  - Status: {statusMessage}");
            Debug.Log($"  - Cost: {config.unlockCost}");
            Debug.Log($"  - Required Level: {config.requiredLevel}");
            Debug.Log($"  - Rarity: {config.rarity}");
        }

        /// <summary>
        /// Display all owned towers
        /// </summary>
        public void DisplayOwnedTowers()
        {
            if (inventoryService == null)
            {
                Debug.LogError("[InventoryExample] Inventory service not available");
                return;
            }

            List<string> ownedTowers = inventoryService.GetAvailableTowers();
            List<string> selectedTowers = inventoryService.GetSelectedTowers();

            Debug.Log($"[InventoryExample] Owned Towers ({ownedTowers.Count}):");
            foreach (string towerName in ownedTowers)
            {
                bool isSelected = selectedTowers.Contains(towerName);
                string marker = isSelected ? "✓" : " ";
                Debug.Log($"  [{marker}] {towerName}");
            }
        }

        /// <summary>
        /// Initialize inventory for a new user
        /// </summary>
        public async void InitializeNewUser()
        {
            if (inventoryService == null)
            {
                Debug.LogError("[InventoryExample] Inventory service not available");
                return;
            }

            Debug.Log($"[InventoryExample] Initializing new user: {testUserId}...");
            bool success = await inventoryService.InitializeUserInventoryAsync(testUserId);

            if (success)
            {
                Debug.Log("[InventoryExample] ✅ User inventory initialized with default towers!");
            }
            else
            {
                Debug.LogWarning("[InventoryExample] User already has inventory or initialization failed");
            }
        }

        #endregion

        #region Helper Methods

        private void DisplayInventoryInfo(TowerInventoryData inventory)
        {
            Debug.Log("═══════════════════════════════════════");
            Debug.Log($"User ID: {inventory.userId}");
            Debug.Log($"Total Towers Owned: {inventory.ownedTowers.Count}");
            Debug.Log($"Selected Towers: {inventory.GetSelectedCount()}/{inventory.maxSelectedTowers}");
            Debug.Log("───────────────────────────────────────");

            foreach (var tower in inventory.ownedTowers)
            {
                string selected = tower.isSelected ? "[SELECTED]" : "";
                Debug.Log($"  {tower.towerName} {selected}");
                Debug.Log($"    Type: {tower.towerType}, Usage: {tower.usageCount}");
            }

            Debug.Log("═══════════════════════════════════════");
        }

        private void DisplayConfigInfo(List<InventoryConfigData> configs)
        {
            Debug.Log("═══════════════════════════════════════");
            Debug.Log($"Total Configurations: {configs.Count}");
            Debug.Log("───────────────────────────────────────");

            // Group by rarity
            Dictionary<int, List<InventoryConfigData>> byRarity = new Dictionary<int, List<InventoryConfigData>>();
            foreach (var config in configs)
            {
                if (!byRarity.ContainsKey(config.rarity))
                {
                    byRarity[config.rarity] = new List<InventoryConfigData>();
                }
                byRarity[config.rarity].Add(config);
            }

            string[] rarityNames = { "Common", "Rare", "Epic", "Legendary" };
            foreach (var kvp in byRarity)
            {
                string rarityName = kvp.Key < rarityNames.Length ? rarityNames[kvp.Key] : $"Rarity {kvp.Key}";
                Debug.Log($"{rarityName} ({kvp.Value.Count}):");
                
                foreach (var config in kvp.Value)
                {
                    string defaultMarker = config.isDefaultUnlocked ? "[DEFAULT]" : "";
                    Debug.Log($"  {config.towerName} {defaultMarker} - Cost: {config.unlockCost}, Level: {config.requiredLevel}");
                }
            }

            Debug.Log("═══════════════════════════════════════");
        }

        #endregion
    }
}


