using System.Collections.Generic;
using System.Threading.Tasks;
using Services.Data;
using TowerDefense.Towers;
using TowerDefense.Towers.Data;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Services.Firestore
{
    /// <summary>
    /// Helper service để load Tower prefabs và populate TowerLibrary từ LevelLibraryConfig
    /// </summary>
    public static class TowerLibraryLoader
    {
        /// <summary>
        /// Cached container instance for runtime access
        /// </summary>
        private static TowerLibraryContainer s_ContainerInstance;

        /// <summary>
        /// Set container instance for runtime access
        /// </summary>
        public static void SetContainerInstance(TowerLibraryContainer container)
        {
            s_ContainerInstance = container;
        }

        /// <summary>
        /// Get container instance (from cache or Resources)
        /// </summary>
        public static TowerLibraryContainer GetContainerInstance()
        {
            if (s_ContainerInstance != null)
            {
                return s_ContainerInstance;
            }

            // Try to load from Resources
            s_ContainerInstance = Resources.Load<TowerLibraryContainer>("TowerLibraryContainer");
            return s_ContainerInstance;
        }

        /// <summary>
        /// Get Tower prefab name from TowerPrefabType enum
        /// Maps enum values to actual prefab names
        /// </summary>
        private static string GetTowerPrefabName(TowerPrefabType towerType)
        {
            switch (towerType)
            {
                case TowerPrefabType.Emp:
                    return "EMP"; // Actual prefab name is EMP.prefab
                case TowerPrefabType.Laser:
                    return "LaserTower";
                case TowerPrefabType.MachineGun:
                    return "MachineGunTower";
                case TowerPrefabType.Pylon:
                    return "EnergyPylon";
                case TowerPrefabType.Rocket:
                    return "RocketTower";
                case TowerPrefabType.SuperTower:
                    return "SuperTower";
                default:
                    return towerType.ToString();
            }
        }

        /// <summary>
        /// Cached all Tower prefabs from Resources/Towers/
        /// </summary>
        private static Tower[] s_CachedTowerPrefabs;
        private static bool s_TowerPrefabsCacheLoaded = false;

        /// <summary>
        /// Load all Tower prefabs from Resources/Towers/ and cache them
        /// </summary>
        private static void LoadAndCacheTowerPrefabs()
        {
            if (s_TowerPrefabsCacheLoaded)
            {
                return;
            }

            s_CachedTowerPrefabs = Resources.LoadAll<Tower>("Towers");
            s_TowerPrefabsCacheLoaded = true;
            Debug.Log($"[TowerLibraryLoader] Loaded and cached {s_CachedTowerPrefabs.Length} Tower prefabs from Resources/Towers/");
        }

        /// <summary>
        /// Load Tower prefab by TowerPrefabType
        /// Finds prefab by matching MainTower enum value (not by name)
        /// Only loads from Resources/Towers/
        /// </summary>
        private static Tower LoadTowerPrefab(TowerPrefabType towerType)
        {
            // Load and cache all tower prefabs if not already loaded
            LoadAndCacheTowerPrefabs();

            if (s_CachedTowerPrefabs == null || s_CachedTowerPrefabs.Length == 0)
            {
                Debug.LogWarning($"[TowerLibraryLoader] No Tower prefabs found in Resources/Towers/");
                return null;
            }

            // Convert TowerPrefabType to MainTower enum (same values)
            TowerDefense.Towers.MainTower targetMainTower = (TowerDefense.Towers.MainTower)towerType;

            // Find prefab with matching MainTower enum
            foreach (Tower prefab in s_CachedTowerPrefabs)
            {
                if (prefab != null && prefab.mainTower == targetMainTower)
                {
                    return prefab;
                }
            }

            Debug.LogWarning($"[TowerLibraryLoader] Tower prefab with MainTower.{targetMainTower} not found in Resources/Towers/");
            return null;
        }

        /// <summary>
        /// Get folder name for tower type
        /// </summary>
        private static string GetTowerFolderName(TowerPrefabType towerType)
        {
            switch (towerType)
            {
                case TowerPrefabType.Emp:
                    return "EMP";
                case TowerPrefabType.Laser:
                    return "Laser";
                case TowerPrefabType.MachineGun:
                    return "MachineGun";
                case TowerPrefabType.Pylon:
                    return "Pylon";
                case TowerPrefabType.Rocket:
                    return "Rocket";
                case TowerPrefabType.SuperTower:
                    return "Supertower";
                default:
                    return towerType.ToString();
            }
        }

        /// <summary>
        /// Load TowerLibrary từ config và populate với Tower prefabs
        /// </summary>
        /// <param name="config">LevelLibraryConfig data từ Firestore</param>
        /// <returns>TowerLibrary đã được populate, hoặc null nếu có lỗi</returns>
        public static TowerLibrary LoadTowerLibraryFromConfig(LevelLibraryConfigData config)
        {
            if (config == null)
            {
                Debug.LogError("[TowerLibraryLoader] Config is null");
                return null;
            }

            // Load TowerLibrary asset từ Resources
            // Path: Resources/TowerLibrary/{towerLibraryPrefabName}
            TowerLibrary library = null;
            if (!string.IsNullOrEmpty(config.towerLibraryPrefabName))
            {
                string resourcePath = $"TowerLibrary/{config.towerLibraryPrefabName}";
                library = Resources.Load<TowerLibrary>(resourcePath);
                
                // Fallback: Try direct name if not found with path
                if (library == null)
                {
                    library = Resources.Load<TowerLibrary>(config.towerLibraryPrefabName);
                }
            }

            // Nếu không tìm thấy, tạo mới
            if (library == null)
            {
                Debug.LogWarning($"[TowerLibraryLoader] TowerLibrary '{config.towerLibraryPrefabName}' not found in Resources, creating new instance");
                library = ScriptableObject.CreateInstance<TowerLibrary>();
            }

            // Clear existing configurations
            if (library.configurations == null)
            {
                library.configurations = new List<Tower>();
            }
            else
            {
                library.configurations.Clear();
            }

            // Load và add towers từ config
            if (config.towerPrefabTypes != null && config.towerPrefabTypes.Count > 0)
            {
                foreach (var towerPrefabTypeValue in config.towerPrefabTypes)
                {
                    if (!DefaultGameData.IsValidTowerPrefabType(towerPrefabTypeValue))
                    {
                        Debug.LogWarning($"[TowerLibraryLoader] Invalid TowerPrefabType: {towerPrefabTypeValue}, skipping...");
                        continue;
                    }

                    TowerPrefabType towerPrefabType = (TowerPrefabType)towerPrefabTypeValue;
                    Tower towerPrefab = LoadTowerPrefab(towerPrefabType);
                    
                    if (towerPrefab != null)
                    {
                        library.configurations.Add(towerPrefab);
                        Debug.Log($"[TowerLibraryLoader] Loaded tower prefab: {GetTowerPrefabName(towerPrefabType)} (Type: {towerPrefabType}) for level {config.levelId}");
                    }
                    else
                    {
                        Debug.LogWarning($"[TowerLibraryLoader] Tower prefab not found for type: {towerPrefabType} (Looking for: {GetTowerPrefabName(towerPrefabType)})");
                    }
                }
            }
            else
            {
                Debug.LogWarning($"[TowerLibraryLoader] No tower prefab types in config for level {config.levelId}");
            }

            // Force rebuild dictionary
            library.OnAfterDeserialize();

            Debug.Log($"[TowerLibraryLoader] Populated TowerLibrary for level {config.levelId} with {library.configurations.Count} towers");
            return library;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Load hoặc tạo TowerLibrary asset trong Editor mode và populate với Tower prefabs
        /// </summary>
        /// <param name="config">LevelLibraryConfig data từ Firestore</param>
        /// <returns>TowerLibrary asset đã được populate và save, hoặc null nếu có lỗi</returns>
        public static TowerLibrary LoadOrCreateTowerLibraryAsset(LevelLibraryConfigData config)
        {
            if (config == null || string.IsNullOrEmpty(config.towerLibraryPrefabName))
            {
                Debug.LogError("[TowerLibraryLoader] Config is null or towerLibraryPrefabName is empty");
                return null;
            }

            // Tạo folder nếu chưa có
            string folderPath = "Assets/Resources/TowerLibrary";
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                {
                    AssetDatabase.CreateFolder("Assets", "Resources");
                }
                AssetDatabase.CreateFolder("Assets/Resources", "TowerLibrary");
            }

            string assetPath = $"{folderPath}/{config.towerLibraryPrefabName}.asset";

            // Load existing hoặc tạo mới
            TowerLibrary library = AssetDatabase.LoadAssetAtPath<TowerLibrary>(assetPath);

            bool isNew = library == null;
            if (library == null)
            {
                library = ScriptableObject.CreateInstance<TowerLibrary>();
                AssetDatabase.CreateAsset(library, assetPath);
                Debug.Log($"[TowerLibraryLoader] Created TowerLibrary asset: {assetPath}");
            }

            // Clear existing configurations
            if (library.configurations == null)
            {
                library.configurations = new List<Tower>();
            }
            else
            {
                library.configurations.Clear();
            }

            // Load và add towers từ config
            int loadedTowerCount = 0;
            if (config.towerPrefabTypes != null && config.towerPrefabTypes.Count > 0)
            {
                foreach (var towerPrefabTypeValue in config.towerPrefabTypes)
                {
                    if (!DefaultGameData.IsValidTowerPrefabType(towerPrefabTypeValue))
                    {
                        Debug.LogWarning($"[TowerLibraryLoader] Invalid TowerPrefabType: {towerPrefabTypeValue}, skipping...");
                        continue;
                    }

                    TowerPrefabType towerPrefabType = (TowerPrefabType)towerPrefabTypeValue;
                    Tower towerPrefab = LoadTowerPrefab(towerPrefabType);
                    
                    if (towerPrefab != null)
                    {
                        library.configurations.Add(towerPrefab);
                        loadedTowerCount++;
                        Debug.Log($"[TowerLibraryLoader] Loaded tower prefab: {GetTowerPrefabName(towerPrefabType)} (Type: {towerPrefabType}) for level {config.levelId}");
                    }
                    else
                    {
                        Debug.LogWarning($"[TowerLibraryLoader] Tower prefab not found for type: {towerPrefabType} (Looking for: {GetTowerPrefabName(towerPrefabType)})");
                    }
                }
            }
            else
            {
                Debug.LogWarning($"[TowerLibraryLoader] No tower prefab types in config for level {config.levelId}");
            }

            // Force rebuild dictionary
            library.OnAfterDeserialize();

            // Mark as dirty and save
            EditorUtility.SetDirty(library);
            AssetDatabase.SaveAssets();

            Debug.Log($"[TowerLibraryLoader] {(isNew ? "Created" : "Updated")} TowerLibrary asset '{config.towerLibraryPrefabName}' for level {config.levelId} with {loadedTowerCount} towers");
            return library;
        }
#endif

        /// <summary>
        /// Load tất cả TowerLibraries từ configs và populate vào TowerLibraryContainer
        /// </summary>
        /// <param name="configs">List các LevelLibraryConfig data từ Firestore</param>
        /// <param name="container">TowerLibraryContainer để chứa các TowerLibrary</param>
        public static void PopulateTowerLibraryContainer(List<LevelLibraryConfigData> configs, TowerLibraryContainer container)
        {
            if (container == null)
            {
                Debug.LogError("[TowerLibraryLoader] Container is null");
                return;
            }

            if (configs == null || configs.Count == 0)
            {
                Debug.LogWarning("[TowerLibraryLoader] No configs provided");
                return;
            }

            // Clear existing
            container.Clear();

            int loadedCount = 0;
            foreach (var config in configs)
            {
                if (config == null || string.IsNullOrEmpty(config.levelId))
                {
                    continue;
                }

                TowerLibrary library;
#if UNITY_EDITOR
                // Trong Editor mode: Tạo/save TowerLibrary assets
                library = LoadOrCreateTowerLibraryAsset(config);
#else
                // Runtime: Load từ Resources hoặc tạo runtime instance
                library = LoadTowerLibraryFromConfig(config);
#endif
                if (library != null)
                {
                    container.SetLibrary(config.levelId, library);
                    loadedCount++;
                }
            }

            Debug.Log($"[TowerLibraryLoader] Populated TowerLibraryContainer with {loadedCount} libraries from {configs.Count} configs");
        }

        /// <summary>
        /// Load TowerLibrary cho một level cụ thể
        /// </summary>
        /// <param name="config">LevelLibraryConfig data</param>
        /// <param name="container">TowerLibraryContainer</param>
        /// <returns>TowerLibrary đã được load, hoặc null nếu không tìm thấy</returns>
        public static TowerLibrary GetTowerLibraryForLevel(LevelLibraryConfigData config, TowerLibraryContainer container)
        {
            if (config == null || container == null)
            {
                return null;
            }

            // Check if already exists in container
            TowerLibrary existingLibrary = container.GetLibrary(config.levelId);
            if (existingLibrary != null && existingLibrary.configurations != null && existingLibrary.configurations.Count > 0)
            {
                return existingLibrary;
            }

            // Load and populate
            TowerLibrary library = LoadTowerLibraryFromConfig(config);
            if (library != null)
            {
                container.SetLibrary(config.levelId, library);
            }

            return library;
        }
    }
}
