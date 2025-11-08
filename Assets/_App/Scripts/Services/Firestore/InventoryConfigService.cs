using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
#if FIREBASE_FIRESTORE_AVAILABLE
using Firebase.Firestore;
#endif
using Services.Core;
using Services.Data;

namespace Services.Firestore
{
    /// <summary>
    /// Microservice for managing Inventory Configuration
    /// Handles configuration for inventory items (unlock costs, requirements, etc.)
    /// </summary>
    public class InventoryConfigService : FirestoreServiceBase, IInventoryConfigService
    {
        private const string DEFAULT_COLLECTION_INVENTORY_CONFIG = "inventoryConfig";
        private string COLLECTION_INVENTORY_CONFIG => DEFAULT_COLLECTION_INVENTORY_CONFIG;

        // Cached configuration data
        private List<InventoryConfigData> cachedConfigs;
        private Dictionary<string, InventoryConfigData> configDictionary;

        // Events
        public event Action<List<InventoryConfigData>> OnInventoryConfigLoaded;

        protected override string GetServiceName() => "InventoryConfigService";

        #region Load Configuration

        public async Task<List<InventoryConfigData>> LoadInventoryConfigAsync()
        {
#if !FIREBASE_FIRESTORE_AVAILABLE
            Debug.LogError($"[{GetServiceName()}] Firebase Firestore package chưa được cài đặt!");
            return new List<InventoryConfigData>();
#else
            if (!isInitialized || firestore == null)
            {
                Debug.LogError($"[{GetServiceName()}] Service not initialized");
                return new List<InventoryConfigData>();
            }

            try
            {
                QuerySnapshot snapshot = await firestore.Collection(COLLECTION_INVENTORY_CONFIG).GetSnapshotAsync();

                Debug.Log($"[{GetServiceName()}] Loading Inventory Config: Found {snapshot.Count} documents");

                List<InventoryConfigData> configs = new List<InventoryConfigData>();
                foreach (DocumentSnapshot document in snapshot.Documents)
                {
                    Dictionary<string, object> data = document.ToDictionary();
                    InventoryConfigData config = ParseConfigData(data);
                    if (config != null)
                    {
                        configs.Add(config);
                    }
                }

                cachedConfigs = configs;
                configDictionary = configs.ToDictionary(c => c.towerName);
                
                OnInventoryConfigLoaded?.Invoke(configs);
                Debug.Log($"[{GetServiceName()}] ✅ Loaded {configs.Count} inventory configurations");
                return configs;
            }
            catch (Exception e)
            {
                string errorMsg = e.Message;
                if (errorMsg.Contains("permission") || errorMsg.Contains("Permission"))
                {
                    Debug.LogError($"[{GetServiceName()}] Permission denied when loading inventory config.");
                }
                else
                {
                    Debug.LogError($"[{GetServiceName()}] Error loading inventory config: {errorMsg}");
                }
                return new List<InventoryConfigData>();
            }
#endif
        }

        public async Task<InventoryConfigData> LoadTowerConfigAsync(string towerName)
        {
#if !FIREBASE_FIRESTORE_AVAILABLE
            Debug.LogError($"[{GetServiceName()}] Firebase Firestore package chưa được cài đặt!");
            return null;
#else
            if (!isInitialized || firestore == null)
            {
                Debug.LogError($"[{GetServiceName()}] Service not initialized");
                return null;
            }

            if (string.IsNullOrEmpty(towerName))
            {
                Debug.LogError($"[{GetServiceName()}] Invalid tower name");
                return null;
            }

            try
            {
                DocumentReference docRef = firestore.Collection(COLLECTION_INVENTORY_CONFIG).Document(towerName);
                DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();

                if (snapshot.Exists)
                {
                    InventoryConfigData config = ParseConfigData(snapshot.ToDictionary());
                    Debug.Log($"[{GetServiceName()}] Loaded config for tower {towerName}");
                    return config;
                }
                else
                {
                    Debug.LogWarning($"[{GetServiceName()}] Config not found for tower {towerName}");
                    return null;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[{GetServiceName()}] Error loading tower config: {e.Message}");
                return null;
            }
#endif
        }

        public List<InventoryConfigData> GetCachedConfigs()
        {
            return cachedConfigs ?? new List<InventoryConfigData>();
        }

        public InventoryConfigData GetTowerConfig(string towerName)
        {
            if (configDictionary != null && configDictionary.ContainsKey(towerName))
            {
                return configDictionary[towerName];
            }
            return null;
        }

        #endregion

        #region Update Configuration

        public async Task<bool> UpdateTowerConfigAsync(InventoryConfigData config)
        {
#if !FIREBASE_FIRESTORE_AVAILABLE
            Debug.LogError($"[{GetServiceName()}] Firebase Firestore package chưa được cài đặt!");
            return false;
#else
            if (!isInitialized || firestore == null)
            {
                Debug.LogError($"[{GetServiceName()}] Service not initialized");
                return false;
            }

            if (config == null || string.IsNullOrEmpty(config.towerName))
            {
                Debug.LogError($"[{GetServiceName()}] Invalid config data");
                return false;
            }

            try
            {
                DocumentReference docRef = firestore.Collection(COLLECTION_INVENTORY_CONFIG).Document(config.towerName);

                Dictionary<string, object> data = new Dictionary<string, object>
                {
                    { "towerName", config.towerName },
                    { "towerType", config.towerType },
                    { "displayName", config.displayName ?? "" },
                    { "description", config.description ?? "" },
                    { "unlockCost", config.unlockCost },
                    { "requiredLevel", config.requiredLevel },
                    { "requiredLevels", config.requiredLevels ?? new List<string>() },
                    { "isDefaultUnlocked", config.isDefaultUnlocked },
                    { "isPurchasable", config.isPurchasable },
                    { "rarity", config.rarity },
                    { "iconName", config.iconName ?? "" },
                    { "sortOrder", config.sortOrder },
                    { "isActive", config.isActive },
                    { "tags", config.tags ?? new List<string>() },
                    { "updatedAt", Timestamp.GetCurrentTimestamp() }
                };

                await docRef.SetAsync(data);

                // Update cache
                if (configDictionary != null)
                {
                    if (configDictionary.ContainsKey(config.towerName))
                    {
                        configDictionary[config.towerName] = config;
                    }
                    else
                    {
                        configDictionary.Add(config.towerName, config);
                    }
                }

                if (cachedConfigs != null)
                {
                    var existingConfig = cachedConfigs.FirstOrDefault(c => c.towerName == config.towerName);
                    if (existingConfig != null)
                    {
                        cachedConfigs.Remove(existingConfig);
                    }
                    cachedConfigs.Add(config);
                }

                Debug.Log($"[{GetServiceName()}] Updated config for tower {config.towerName}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[{GetServiceName()}] Error updating config: {e.Message}");
                return false;
            }
#endif
        }

        #endregion

        #region Query Methods

        public bool CanUnlockTower(string towerName, int userLevel, int userCurrency)
        {
            var config = GetTowerConfig(towerName);
            if (config == null)
            {
                return false;
            }

            // For this method, we'll check basic requirements (level and currency)
            // Level requirements check would need user's level progress data
            return config.isActive && 
                   config.isPurchasable && 
                   userLevel >= config.requiredLevel && 
                   userCurrency >= config.unlockCost;
        }

        public int GetUnlockCost(string towerName)
        {
            var config = GetTowerConfig(towerName);
            return config?.unlockCost ?? -1;
        }

        #endregion

        #region Initialize

        public async Task<bool> InitializeCollectionIfEmptyAsync()
        {
#if !FIREBASE_FIRESTORE_AVAILABLE
            Debug.LogError($"[{GetServiceName()}] Firebase Firestore package chưa được cài đặt!");
            return false;
#else
            if (!isInitialized || firestore == null)
            {
                Debug.LogError($"[{GetServiceName()}] Service not initialized");
                return false;
            }

            try
            {
                // Check if collection is empty
                QuerySnapshot existingSnapshot = await firestore.Collection(COLLECTION_INVENTORY_CONFIG)
                    .Limit(1)
                    .GetSnapshotAsync();

                if (existingSnapshot.Count > 0)
                {
                    Debug.Log($"[{GetServiceName()}] Collection {COLLECTION_INVENTORY_CONFIG} is not empty, skipping initialization");
                    return true;
                }

                Debug.Log($"[{GetServiceName()}] Collection {COLLECTION_INVENTORY_CONFIG} is empty, initializing...");

                // Create default configurations
                List<InventoryConfigData> defaultConfigs = CreateDefaultConfigurations();

                foreach (var config in defaultConfigs)
                {
                    await UpdateTowerConfigAsync(config);
                }

                Debug.Log($"[{GetServiceName()}] ✅ Initialized {defaultConfigs.Count} default configurations");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[{GetServiceName()}] Error initializing collection: {e.Message}");
                return false;
            }
#endif
        }

        #endregion

        #region Helper Methods

#if FIREBASE_FIRESTORE_AVAILABLE
        private InventoryConfigData ParseConfigData(Dictionary<string, object> data)
        {
            try
            {
                InventoryConfigData config = new InventoryConfigData();

                if (data.ContainsKey("towerName"))
                    config.towerName = data["towerName"].ToString();

                if (data.ContainsKey("towerType"))
                    config.towerType = Convert.ToInt32(data["towerType"]);

                if (data.ContainsKey("displayName"))
                    config.displayName = data["displayName"].ToString();

                if (data.ContainsKey("description"))
                    config.description = data["description"].ToString();

                if (data.ContainsKey("unlockCost"))
                    config.unlockCost = Convert.ToInt32(data["unlockCost"]);

                if (data.ContainsKey("requiredLevel"))
                    config.requiredLevel = Convert.ToInt32(data["requiredLevel"]);

                if (data.ContainsKey("requiredLevels") && data["requiredLevels"] is List<object> reqLevels)
                {
                    config.requiredLevels = reqLevels.Select(l => l.ToString()).ToList();
                }

                if (data.ContainsKey("isDefaultUnlocked"))
                    config.isDefaultUnlocked = Convert.ToBoolean(data["isDefaultUnlocked"]);

                if (data.ContainsKey("isPurchasable"))
                    config.isPurchasable = Convert.ToBoolean(data["isPurchasable"]);

                if (data.ContainsKey("rarity"))
                    config.rarity = Convert.ToInt32(data["rarity"]);

                if (data.ContainsKey("iconName"))
                    config.iconName = data["iconName"].ToString();

                if (data.ContainsKey("sortOrder"))
                    config.sortOrder = Convert.ToInt32(data["sortOrder"]);

                if (data.ContainsKey("isActive"))
                    config.isActive = Convert.ToBoolean(data["isActive"]);

                if (data.ContainsKey("tags") && data["tags"] is List<object> tagsList)
                {
                    config.tags = tagsList.Select(t => t.ToString()).ToList();
                }

                return config;
            }
            catch (Exception e)
            {
                Debug.LogError($"[{GetServiceName()}] Error parsing config data: {e.Message}");
                return null;
            }
        }
#endif

        private List<InventoryConfigData> CreateDefaultConfigurations()
        {
            List<InventoryConfigData> configs = new List<InventoryConfigData>();

            // MachineGun towers
            configs.Add(new InventoryConfigData("MachineGun1", 2)
            {
                displayName = "Machine Gun I",
                description = "Basic rapid-fire tower",
                unlockCost = 0,
                requiredLevel = 0,
                isDefaultUnlocked = true,
                isPurchasable = false,
                rarity = 0,
                sortOrder = 0
            });

            configs.Add(new InventoryConfigData("MachineGun2", 2)
            {
                displayName = "Machine Gun II",
                description = "Improved machine gun with better fire rate",
                unlockCost = 1000,
                requiredLevel = 3,
                isDefaultUnlocked = false,
                isPurchasable = true,
                rarity = 1,
                sortOrder = 1
            });

            configs.Add(new InventoryConfigData("MachineGun3", 2)
            {
                displayName = "Machine Gun III",
                description = "Advanced machine gun with superior damage",
                unlockCost = 2500,
                requiredLevel = 8,
                isDefaultUnlocked = false,
                isPurchasable = true,
                rarity = 2,
                sortOrder = 2
            });

            // Laser towers
            configs.Add(new InventoryConfigData("Laser1", 1)
            {
                displayName = "Laser I",
                description = "Energy-based tower with precision targeting",
                unlockCost = 1500,
                requiredLevel = 5,
                isDefaultUnlocked = false,
                isPurchasable = true,
                rarity = 1,
                sortOrder = 3
            });

            configs.Add(new InventoryConfigData("Laser2", 1)
            {
                displayName = "Laser II",
                description = "Enhanced laser with increased damage",
                unlockCost = 3000,
                requiredLevel = 10,
                isDefaultUnlocked = false,
                isPurchasable = true,
                rarity = 2,
                sortOrder = 4
            });

            configs.Add(new InventoryConfigData("Laser3", 1)
            {
                displayName = "Laser III",
                description = "Ultimate laser tower with devastating power",
                unlockCost = 5000,
                requiredLevel = 15,
                isDefaultUnlocked = false,
                isPurchasable = true,
                rarity = 3,
                sortOrder = 5
            });

            // Rocket towers
            configs.Add(new InventoryConfigData("Rocket1", 4)
            {
                displayName = "Rocket I",
                description = "Explosive area damage tower",
                unlockCost = 2000,
                requiredLevel = 7,
                isDefaultUnlocked = false,
                isPurchasable = true,
                rarity = 1,
                sortOrder = 6
            });

            configs.Add(new InventoryConfigData("Rocket2", 4)
            {
                displayName = "Rocket II",
                description = "Heavy rocket launcher with splash damage",
                unlockCost = 4000,
                requiredLevel = 12,
                isDefaultUnlocked = false,
                isPurchasable = true,
                rarity = 2,
                sortOrder = 7
            });

            configs.Add(new InventoryConfigData("Rocket3", 4)
            {
                displayName = "Rocket III",
                description = "Advanced missile system",
                unlockCost = 6000,
                requiredLevel = 18,
                isDefaultUnlocked = false,
                isPurchasable = true,
                rarity = 3,
                sortOrder = 8
            });

            // EMP towers
            configs.Add(new InventoryConfigData("Emp1", 0)
            {
                displayName = "EMP I",
                description = "Slows down enemies with electromagnetic pulses",
                unlockCost = 1800,
                requiredLevel = 6,
                isDefaultUnlocked = false,
                isPurchasable = true,
                rarity = 1,
                sortOrder = 9
            });

            configs.Add(new InventoryConfigData("Emp2", 0)
            {
                displayName = "EMP II",
                description = "Enhanced EMP with longer stun duration",
                unlockCost = 3500,
                requiredLevel = 11,
                isDefaultUnlocked = false,
                isPurchasable = true,
                rarity = 2,
                sortOrder = 10
            });

            configs.Add(new InventoryConfigData("Emp3", 0)
            {
                displayName = "EMP III",
                description = "Ultimate EMP system",
                unlockCost = 5500,
                requiredLevel = 16,
                isDefaultUnlocked = false,
                isPurchasable = true,
                rarity = 3,
                sortOrder = 11
            });

            // Pylon towers
            configs.Add(new InventoryConfigData("Pylon1", 3)
            {
                displayName = "Pylon I",
                description = "Support tower that boosts nearby towers",
                unlockCost = 2200,
                requiredLevel = 9,
                isDefaultUnlocked = false,
                isPurchasable = true,
                rarity = 2,
                sortOrder = 12
            });

            configs.Add(new InventoryConfigData("Pylon2", 3)
            {
                displayName = "Pylon II",
                description = "Advanced support pylon",
                unlockCost = 4500,
                requiredLevel = 14,
                isDefaultUnlocked = false,
                isPurchasable = true,
                rarity = 2,
                sortOrder = 13
            });

            configs.Add(new InventoryConfigData("Pylon3", 3)
            {
                displayName = "Pylon III",
                description = "Supreme support tower",
                unlockCost = 7000,
                requiredLevel = 20,
                isDefaultUnlocked = false,
                isPurchasable = true,
                rarity = 3,
                sortOrder = 14
            });

            // Super Tower
            configs.Add(new InventoryConfigData("SuperTower", 5)
            {
                displayName = "Super Tower",
                description = "Legendary all-purpose tower",
                unlockCost = 10000,
                requiredLevel = 25,
                isDefaultUnlocked = false,
                isPurchasable = true,
                rarity = 3,
                sortOrder = 15
            });

            return configs;
        }

        #endregion
    }
}

