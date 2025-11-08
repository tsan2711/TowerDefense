using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Services.Data;
using TowerDefense.Towers.Data;
using TowerDefense.Agents.Data;
using Core.Game;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Services.Firestore
{
    /// <summary>
    /// Service để sync data từ Firestore vào ScriptableObject
    /// Tự động tạo và update ScriptableObjects khi data được load từ Firestore
    /// </summary>
    public static class GameDataSyncService
    {
        /// <summary>
        /// Sync TowerLevelData từ Firestore vào ScriptableObjects
        /// Tạo hoặc update ScriptableObjects trong Resources/TowerData/
        /// </summary>
        public static void SyncTowerLevelDataToScriptableObjects(List<TowerLevelDataData> firestoreData)
        {
            if (firestoreData == null || firestoreData.Count == 0)
            {
                Debug.LogWarning("[GameDataSyncService] No tower data to sync");
                return;
            }

#if UNITY_EDITOR
            // Tạo folder nếu chưa có
            string folderPath = "Assets/Resources/TowerData";
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                {
                    AssetDatabase.CreateFolder("Assets", "Resources");
                }
                AssetDatabase.CreateFolder("Assets/Resources", "TowerData");
            }

            int syncedCount = 0;
            int createdCount = 0;
            int updatedCount = 0;

            foreach (var data in firestoreData)
            {
                try
                {
                    TowerType towerType = (TowerType)data.type;
                    string assetPath = $"{folderPath}/TowerLevelData_{towerType}.asset";

                    // Load existing hoặc tạo mới
                    TowerLevelData scriptableObject = AssetDatabase.LoadAssetAtPath<TowerLevelData>(assetPath);

                    if (scriptableObject == null)
                    {
                        scriptableObject = ScriptableObject.CreateInstance<TowerLevelData>();
                        AssetDatabase.CreateAsset(scriptableObject, assetPath);
                        createdCount++;
                        Debug.Log($"[GameDataSyncService] Created ScriptableObject: {assetPath}");
                    }
                    else
                    {
                        updatedCount++;
                        Debug.Log($"[GameDataSyncService] Updating ScriptableObject: {assetPath}");
                    }

                    // Update data từ Firestore
                    scriptableObject.towerType = towerType;
                    scriptableObject.description = data.description ?? "";
                    scriptableObject.upgradeDescription = data.upgradeDescription ?? "";
                    scriptableObject.cost = data.cost;
                    scriptableObject.sell = data.sell;
                    scriptableObject.maxHealth = data.maxHealth;
                    scriptableObject.startingHealth = data.startingHealth;
                    // Note: icon không được sync vì cần reference từ Unity (phải set manual trong Editor)

                    EditorUtility.SetDirty(scriptableObject);
                    syncedCount++;
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[GameDataSyncService] Error syncing tower type {data.type}: {ex.Message}");
                    Debug.LogError($"[GameDataSyncService] Exception: {ex}");
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[GameDataSyncService] ✅ Synced {syncedCount} tower ScriptableObjects ({createdCount} created, {updatedCount} updated)");
#else
            Debug.LogWarning("[GameDataSyncService] ScriptableObject sync chỉ hoạt động trong Editor mode. " +
                           "Runtime: Sử dụng data từ cache thay vì ScriptableObject.");
#endif
        }

        /// <summary>
        /// Sync AgentConfiguration từ Firestore vào ScriptableObjects
        /// </summary>
        public static void SyncAgentConfigurationsToScriptableObjects(List<AgentConfigurationData> firestoreData)
        {
            if (firestoreData == null || firestoreData.Count == 0)
            {
                Debug.LogWarning("[GameDataSyncService] No agent data to sync");
                return;
            }

#if UNITY_EDITOR
            string folderPath = "Assets/Resources/AgentData";
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                {
                    AssetDatabase.CreateFolder("Assets", "Resources");
                }
                AssetDatabase.CreateFolder("Assets/Resources", "AgentData");
            }

            int syncedCount = 0;
            int createdCount = 0;
            int updatedCount = 0;

            foreach (var data in firestoreData)
            {
                try
                {
                    AgentType agentType = (AgentType)data.type;
                    string assetPath = $"{folderPath}/AgentConfiguration_{agentType}.asset";

                    AgentConfiguration scriptableObject = AssetDatabase.LoadAssetAtPath<AgentConfiguration>(assetPath);

                    if (scriptableObject == null)
                    {
                        scriptableObject = ScriptableObject.CreateInstance<AgentConfiguration>();
                        AssetDatabase.CreateAsset(scriptableObject, assetPath);
                        createdCount++;
                        Debug.Log($"[GameDataSyncService] Created ScriptableObject: {assetPath}");
                    }
                    else
                    {
                        updatedCount++;
                        Debug.Log($"[GameDataSyncService] Updating ScriptableObject: {assetPath}");
                    }

                    scriptableObject.agentType = agentType;
                    scriptableObject.agentName = data.agentName ?? "";
                    scriptableObject.agentDescription = data.agentDescription ?? "";
                    // Note: agentPrefab không được sync vì cần reference từ Unity (phải set manual trong Editor)

                    EditorUtility.SetDirty(scriptableObject);
                    syncedCount++;
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[GameDataSyncService] Error syncing agent type {data.type}: {ex.Message}");
                    Debug.LogError($"[GameDataSyncService] Exception: {ex}");
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[GameDataSyncService] ✅ Synced {syncedCount} agent ScriptableObjects ({createdCount} created, {updatedCount} updated)");
#else
            Debug.LogWarning("[GameDataSyncService] ScriptableObject sync chỉ hoạt động trong Editor mode.");
#endif
        }

        /// <summary>
        /// Sync LevelList từ Firestore vào ScriptableObject
        /// </summary>
        public static void SyncLevelListToScriptableObject(LevelListData firestoreData)
        {
            if (firestoreData == null || firestoreData.levels == null)
            {
                Debug.LogWarning("[GameDataSyncService] No level list data to sync");
                return;
            }

#if UNITY_EDITOR
            string folderPath = "Assets/Resources";
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }

            string assetPath = $"{folderPath}/LevelList.asset";
            LevelList scriptableObject = AssetDatabase.LoadAssetAtPath<LevelList>(assetPath);

            bool isNew = scriptableObject == null;
            if (scriptableObject == null)
            {
                scriptableObject = ScriptableObject.CreateInstance<LevelList>();
                AssetDatabase.CreateAsset(scriptableObject, assetPath);
            }

            // Convert LevelItemData sang LevelItem
            List<LevelItem> levelItems = new List<LevelItem>();
            foreach (var itemData in firestoreData.levels)
            {
                levelItems.Add(new LevelItem
                {
                    id = itemData.id ?? "",
                    name = itemData.name ?? "",
                    description = itemData.description ?? "",
                    sceneName = itemData.sceneName ?? ""
                });
            }

            scriptableObject.levels = levelItems.ToArray();
            EditorUtility.SetDirty(scriptableObject);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[GameDataSyncService] ✅ Synced LevelList ScriptableObject with {levelItems.Count} levels ({(isNew ? "Created" : "Updated")})");
#else
            Debug.LogWarning("[GameDataSyncService] ScriptableObject sync chỉ hoạt động trong Editor mode.");
#endif
        }

        /// <summary>
        /// Sync LevelLibraryConfig vào TowerLibraryContainer
        /// Load Tower prefabs từ Resources/Tower và populate vào các TowerLibrary
        /// </summary>
        public static void SyncLevelLibraryConfigsToContainer(List<LevelLibraryConfigData> configs)
        {
            if (configs == null || configs.Count == 0)
            {
                Debug.LogWarning("[GameDataSyncService] No LevelLibraryConfig data to sync");
                return;
            }

            // Load hoặc tạo TowerLibraryContainer
            TowerLibraryContainer container = null;
            
#if UNITY_EDITOR
            // Trong Editor: Load từ AssetDatabase để có reference chính xác
            string containerAssetPath = "Assets/Resources/TowerLibraryContainer.asset";
            container = AssetDatabase.LoadAssetAtPath<TowerLibraryContainer>(containerAssetPath);
            
            if (container == null)
            {
                Debug.LogWarning("[GameDataSyncService] TowerLibraryContainer not found, creating new one...");
                
                // Tạo folder nếu chưa có
                string folderPath = "Assets/Resources";
                if (!AssetDatabase.IsValidFolder(folderPath))
                {
                    AssetDatabase.CreateFolder("Assets", "Resources");
                }

                container = ScriptableObject.CreateInstance<TowerLibraryContainer>();
                AssetDatabase.CreateAsset(container, containerAssetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"[GameDataSyncService] Created TowerLibraryContainer: {containerAssetPath}");
            }
            else
            {
                Debug.Log($"[GameDataSyncService] Loaded existing TowerLibraryContainer from: {containerAssetPath}");
            }
#else
            // Runtime: Load từ Resources
            container = Resources.Load<TowerLibraryContainer>("TowerLibraryContainer");
            
            if (container == null)
            {
                Debug.LogWarning("[GameDataSyncService] TowerLibraryContainer not found in Resources, creating temporary instance in memory");
                container = ScriptableObject.CreateInstance<TowerLibraryContainer>();
            }
#endif

            // Populate container với TowerLibraries
            TowerLibraryLoader.PopulateTowerLibraryContainer(configs, container);

#if UNITY_EDITOR
            if (container != null)
            {
                EditorUtility.SetDirty(container);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
#endif

            // Store container reference for runtime access
            TowerLibraryLoader.SetContainerInstance(container);

            Debug.Log($"[GameDataSyncService] ✅ Synced TowerLibraryContainer with {configs.Count} level library configs");
        }

        /// <summary>
        /// Sync user inventory data vào TowerInventory ScriptableObject
        /// Load TowerInventory từ Resources hoặc tạo mới nếu chưa có
        /// </summary>
        public static void SyncUserInventoryToScriptableObject(TowerInventoryData inventoryData)
        {
            if (inventoryData == null)
            {
                Debug.LogWarning("[GameDataSyncService] No inventory data to sync");
                return;
            }

            TowerInventory towerInventory = null;
            string assetPath = null;

#if UNITY_EDITOR
            // Trong Editor: Try load từ AssetDatabase trước
            string[] assetPaths = new string[]
            {
                "Assets/Resources/TowerInventory.asset",
                "Assets/Resources/PlayerTowerInventory.asset",
                "Assets/Data/TowerInventory.asset",
                "Assets/_App/Data/TowerInventory.asset"
            };

            foreach (string path in assetPaths)
            {
                towerInventory = AssetDatabase.LoadAssetAtPath<TowerInventory>(path);
                if (towerInventory != null)
                {
                    assetPath = path;
                    Debug.Log($"[GameDataSyncService] Found TowerInventory at: {assetPath}");
                    break;
                }
            }
#endif

            // Nếu không tìm thấy trong Editor, thử load từ Resources
            if (towerInventory == null)
            {
                towerInventory = Resources.Load<TowerInventory>("TowerInventory");
                if (towerInventory == null)
                {
                    towerInventory = Resources.Load<TowerInventory>("PlayerTowerInventory");
                }
            }

#if UNITY_EDITOR
            // Nếu vẫn chưa có, tạo mới trong Resources folder
            if (towerInventory == null)
            {
                Debug.Log("[GameDataSyncService] TowerInventory not found, creating new one in Resources folder...");
                
                // Tạo folder Resources nếu chưa có
                string resourcesFolder = "Assets/Resources";
                if (!AssetDatabase.IsValidFolder(resourcesFolder))
                {
                    AssetDatabase.CreateFolder("Assets", "Resources");
                    Debug.Log("[GameDataSyncService] Created Resources folder");
                }

                // Tạo TowerInventory asset
                assetPath = $"{resourcesFolder}/TowerInventory.asset";
                towerInventory = ScriptableObject.CreateInstance<TowerInventory>();
                
                // Tìm và assign TowerLibrary reference
                TowerLibrary towerLibrary = FindTowerLibrary();
                if (towerLibrary != null)
                {
                    towerInventory.towerLibrary = towerLibrary;
                    Debug.Log($"[GameDataSyncService] Assigned TowerLibrary reference to TowerInventory");
                }
                else
                {
                    Debug.LogWarning("[GameDataSyncService] TowerLibrary not found. Please assign it manually in Inspector.");
                }

                AssetDatabase.CreateAsset(towerInventory, assetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                
                Debug.Log($"[GameDataSyncService] ✅ Created TowerInventory at: {assetPath}");
            }
#endif

            if (towerInventory == null)
            {
                Debug.LogError("[GameDataSyncService] Failed to create or load TowerInventory ScriptableObject. " +
                               "Please create one manually: Create > TowerDefense > Tower Inventory and place it in Resources folder.");
                return;
            }

            // Đảm bảo ownedTowers không null trước khi sync
            if (inventoryData.ownedTowers == null)
            {
                inventoryData.ownedTowers = new List<InventoryItemData>();
                Debug.LogWarning("[GameDataSyncService] inventoryData.ownedTowers was null, initialized empty list");
            }

            // Sync data vào ScriptableObject
            towerInventory.SyncWithInventoryData(inventoryData);
            
#if UNITY_EDITOR
            EditorUtility.SetDirty(towerInventory);
            AssetDatabase.SaveAssets();
#endif

            int selectedCount = inventoryData.GetSelectedCount();
            int ownedCount = inventoryData.ownedTowers?.Count ?? 0;
            Debug.Log($"[GameDataSyncService] ✅ Synced TowerInventory ScriptableObject: {selectedCount} towers selected, {ownedCount} towers owned");
        }

#if UNITY_EDITOR
        /// <summary>
        /// Tìm TowerLibrary từ Resources hoặc AssetDatabase
        /// </summary>
        private static TowerLibrary FindTowerLibrary()
        {
            // Thử load từ Resources
            TowerLibrary library = Resources.Load<TowerLibrary>("TowerLibrary");
            if (library != null)
            {
                return library;
            }

            // Thử tìm trong AssetDatabase
            string[] guids = AssetDatabase.FindAssets("t:TowerLibrary");
            if (guids.Length > 0)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                library = AssetDatabase.LoadAssetAtPath<TowerLibrary>(assetPath);
                if (library != null)
                {
                    Debug.Log($"[GameDataSyncService] Found TowerLibrary at: {assetPath}");
                    return library;
                }
            }

            // Thử load từ TowerLibraryContainer nếu có
            TowerLibraryContainer container = TowerLibraryLoader.GetContainerInstance();
            if (container != null)
            {
                List<TowerLibrary> allLibraries = container.GetAllLibraries();
                if (allLibraries != null && allLibraries.Count > 0)
                {
                    // Lấy TowerLibrary đầu tiên từ container
                    foreach (var levelLibrary in allLibraries)
                    {
                        if (levelLibrary != null)
                        {
                            Debug.Log("[GameDataSyncService] Using TowerLibrary from TowerLibraryContainer");
                            return levelLibrary;
                        }
                    }
                }
            }

            Debug.LogWarning("[GameDataSyncService] TowerLibrary not found. Please create one manually.");
            return null;
        }
#endif

        /// <summary>
        /// Sync inventory config data vào InventoryConfigScriptableObject
        /// Tự động tạo nếu chưa có
        /// </summary>
        public static void SyncInventoryConfigToScriptableObject(List<InventoryConfigData> configs)
        {
            if (configs == null || configs.Count == 0)
            {
                Debug.LogWarning("[GameDataSyncService] No inventory config data to sync");
                return;
            }

#if UNITY_EDITOR
            // Tạo folder Resources nếu chưa có
            string resourcesFolder = "Assets/Resources";
            if (!AssetDatabase.IsValidFolder(resourcesFolder))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }

            string assetPath = $"{resourcesFolder}/InventoryConfig.asset";
            InventoryConfigScriptableObject configSO = AssetDatabase.LoadAssetAtPath<InventoryConfigScriptableObject>(assetPath);

            bool isNew = configSO == null;
            if (configSO == null)
            {
                configSO = ScriptableObject.CreateInstance<InventoryConfigScriptableObject>();
                AssetDatabase.CreateAsset(configSO, assetPath);
                Debug.Log($"[GameDataSyncService] Created InventoryConfigScriptableObject at: {assetPath}");
            }

            // Sync configs
            configSO.SyncConfigs(configs);

            EditorUtility.SetDirty(configSO);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[GameDataSyncService] ✅ Synced InventoryConfigScriptableObject with {configs.Count} configs ({(isNew ? "Created" : "Updated")})");
#else
            // Runtime: Load từ Resources
            InventoryConfigScriptableObject configSO = Resources.Load<InventoryConfigScriptableObject>("InventoryConfig");
            if (configSO != null)
            {
                configSO.SyncConfigs(configs);
                Debug.Log($"[GameDataSyncService] ✅ Synced InventoryConfigScriptableObject with {configs.Count} configs (Runtime)");
            }
            else
            {
                Debug.LogWarning("[GameDataSyncService] InventoryConfigScriptableObject not found in Resources");
            }
#endif
        }

        /// <summary>
        /// Sync user inventory data vào UserInventoryScriptableObject
        /// Tự động tạo nếu chưa có (chỉ trong Editor/Play mode)
        /// </summary>
        public static void SyncUserInventoryDataToScriptableObject(TowerInventoryData inventoryData)
        {
            Debug.Log("[GameDataSyncService] SyncUserInventoryDataToScriptableObject called");
            
            if (inventoryData == null)
            {
                Debug.LogWarning("[GameDataSyncService] No inventory data to sync - inventoryData is null");
                return;
            }

            Debug.Log($"[GameDataSyncService] Syncing inventory for user: {inventoryData.userId}, ownedTowers: {inventoryData.ownedTowers?.Count ?? 0}");

            UserInventoryScriptableObject userInventorySO = null;
            bool isNew = false;

#if UNITY_EDITOR
            try
            {
                // Editor/Play mode: Tự động tạo ScriptableObject nếu chưa có
                string resourcesFolder = "Assets/Resources";
                if (!AssetDatabase.IsValidFolder(resourcesFolder))
                {
                    AssetDatabase.CreateFolder("Assets", "Resources");
                    Debug.Log("[GameDataSyncService] Created Resources folder");
                }

                string assetPath = $"{resourcesFolder}/UserInventory.asset";
                Debug.Log($"[GameDataSyncService] Looking for UserInventoryScriptableObject at: {assetPath}");
                
                userInventorySO = AssetDatabase.LoadAssetAtPath<UserInventoryScriptableObject>(assetPath);

                if (userInventorySO == null)
                {
                    // Tạo mới
                    Debug.Log("[GameDataSyncService] UserInventoryScriptableObject not found, creating new one...");
                    userInventorySO = ScriptableObject.CreateInstance<UserInventoryScriptableObject>();
                    
                    if (userInventorySO == null)
                    {
                        Debug.LogError("[GameDataSyncService] Failed to create UserInventoryScriptableObject instance!");
                        return;
                    }
                    
                    AssetDatabase.CreateAsset(userInventorySO, assetPath);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                    isNew = true;
                    Debug.Log($"[GameDataSyncService] ✅ Created UserInventoryScriptableObject at: {assetPath}");
                }
                else
                {
                    Debug.Log($"[GameDataSyncService] Found existing UserInventoryScriptableObject at: {assetPath}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameDataSyncService] Error creating/loading UserInventoryScriptableObject in Editor: {ex.Message}");
                Debug.LogError($"[GameDataSyncService] Stack trace: {ex.StackTrace}");
                return;
            }
#else
            // Runtime (built game): Chỉ load từ Resources
            userInventorySO = Resources.Load<UserInventoryScriptableObject>("UserInventory");
            if (userInventorySO == null)
            {
                Debug.LogWarning("[GameDataSyncService] UserInventoryScriptableObject not found in Resources. " +
                               "In built games, ScriptableObjects must be pre-generated. " +
                               "Creating temporary instance in memory...");
                // Tạo temporary instance trong memory (không persist)
                userInventorySO = ScriptableObject.CreateInstance<UserInventoryScriptableObject>();
            }
#endif

            // Sync data vào ScriptableObject
            if (userInventorySO != null)
            {
                try
                {
                    // Đảm bảo ownedTowers không null trước khi sync
                    if (inventoryData.ownedTowers == null)
                    {
                        inventoryData.ownedTowers = new List<InventoryItemData>();
                        Debug.LogWarning("[GameDataSyncService] inventoryData.ownedTowers was null, initialized empty list");
                    }

                    Debug.Log($"[GameDataSyncService] Calling SyncFromInventoryData...");
                    userInventorySO.SyncFromInventoryData(inventoryData);

                    int ownedCount = inventoryData.ownedTowers?.Count ?? 0;
                    int selectedCount = inventoryData.GetSelectedCount();

#if UNITY_EDITOR
                    EditorUtility.SetDirty(userInventorySO);
                    AssetDatabase.SaveAssets();
                    Debug.Log($"[GameDataSyncService] ✅ Synced UserInventoryScriptableObject: {ownedCount} towers owned, {selectedCount} selected ({(isNew ? "Created" : "Updated")})");
#else
                    Debug.Log($"[GameDataSyncService] ✅ Synced UserInventoryScriptableObject (Runtime): {ownedCount} towers owned, {selectedCount} selected");
#endif
                }
                catch (Exception syncEx)
                {
                    Debug.LogError($"[GameDataSyncService] Error syncing data to UserInventoryScriptableObject: {syncEx.Message}");
                    Debug.LogError($"[GameDataSyncService] Stack trace: {syncEx.StackTrace}");
                }
            }
            else
            {
                Debug.LogError("[GameDataSyncService] Failed to create or load UserInventoryScriptableObject - userInventorySO is null");
            }
        }
    }
}

