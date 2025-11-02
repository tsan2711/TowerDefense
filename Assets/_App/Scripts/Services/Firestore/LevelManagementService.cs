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
using Services.Config;
using Services.Managers;

namespace Services.Firestore
{
    /// <summary>
    /// Microservice for managing Level List and Level Library Config
    /// Handles only Level Management domain operations
    /// </summary>
    public class LevelManagementService : FirestoreServiceBase, ILevelManagementService
    {
        private const string DEFAULT_COLLECTION_LEVEL_LIST = "LevelList";
        private const string DEFAULT_COLLECTION_LEVEL_LIBRARY_CONFIG = "LevelLibraryConfig";
        
        private string COLLECTION_LEVEL_LIST => 
            FirebaseConfigManager.Instance?.GetLevelListCollection() ?? DEFAULT_COLLECTION_LEVEL_LIST;
        private string COLLECTION_LEVEL_LIBRARY_CONFIG => 
            FirebaseConfigManager.Instance?.GetLevelLibraryConfigCollection() ?? DEFAULT_COLLECTION_LEVEL_LIBRARY_CONFIG;

        private LevelListData cachedLevelList;
        private List<LevelLibraryConfigData> cachedLevelLibraryConfigs;

        public event Action<LevelListData> OnLevelListLoaded;
        public event Action<List<LevelLibraryConfigData>> OnLevelLibraryConfigsLoaded;

        protected override string GetServiceName() => "LevelManagementService";

        public async Task<LevelListData> LoadLevelListAsync()
        {
#if !FIREBASE_FIRESTORE_AVAILABLE
            Debug.LogError($"[{GetServiceName()}] Firebase Firestore package chưa được cài đặt!");
            return new LevelListData();
#else
            if (!isInitialized || firestore == null)
            {
                Debug.LogError($"[{GetServiceName()}] Service not initialized");
                return new LevelListData();
            }

            try
            {
                DocumentReference docRef = firestore.Collection(COLLECTION_LEVEL_LIST).Document("main");
                DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();

                if (snapshot.Exists)
                {
                    Dictionary<string, object> data = snapshot.ToDictionary();
                    LevelListData levelList = ParseLevelList(data);
                    cachedLevelList = levelList;
                    OnLevelListLoaded?.Invoke(levelList);
                    Debug.Log($"[{GetServiceName()}] Loaded LevelList with {levelList.levels.Count} levels");
                    return levelList;
                }
                else
                {
                    Debug.LogWarning($"[{GetServiceName()}] LevelList document not found");
                    return new LevelListData();
                }
            }
            catch (Exception e)
            {
                string errorMsg = e.Message;
                if (errorMsg.Contains("permission") || errorMsg.Contains("Permission"))
                {
                    Debug.LogError($"[{GetServiceName()}] Permission denied when loading LevelList.");
                }
                else
                {
                    Debug.LogError($"[{GetServiceName()}] Error loading LevelList: {errorMsg}");
                }
                return new LevelListData();
            }
#endif
        }

        public async Task<List<LevelLibraryConfigData>> LoadLevelLibraryConfigsAsync()
        {
#if !FIREBASE_FIRESTORE_AVAILABLE
            Debug.LogError($"[{GetServiceName()}] Firebase Firestore package chưa được cài đặt!");
            return new List<LevelLibraryConfigData>();
#else
            if (!isInitialized || firestore == null)
            {
                Debug.LogError($"[{GetServiceName()}] Service not initialized");
                return new List<LevelLibraryConfigData>();
            }

            try
            {
                QuerySnapshot snapshot = await firestore.Collection(COLLECTION_LEVEL_LIBRARY_CONFIG).GetSnapshotAsync();

                List<LevelLibraryConfigData> configs = new List<LevelLibraryConfigData>();
                foreach (DocumentSnapshot document in snapshot.Documents)
                {
                    Dictionary<string, object> data = document.ToDictionary();
                    LevelLibraryConfigData config = ParseLevelLibraryConfig(data);
                    if (config != null)
                    {
                        configs.Add(config);
                    }
                }

                cachedLevelLibraryConfigs = configs;
                OnLevelLibraryConfigsLoaded?.Invoke(configs);
                Debug.Log($"[{GetServiceName()}] Loaded {configs.Count} LevelLibraryConfigs");
                return configs;
            }
            catch (Exception e)
            {
                string errorMsg = e.Message;
                if (errorMsg.Contains("permission") || errorMsg.Contains("Permission"))
                {
                    Debug.LogError($"[{GetServiceName()}] Permission denied when loading LevelLibraryConfig.");
                }
                else
                {
                    Debug.LogError($"[{GetServiceName()}] Error loading LevelLibraryConfig: {errorMsg}");
                }
                return new List<LevelLibraryConfigData>();
            }
#endif
        }

        public async Task<LevelLibraryConfigData> LoadLevelLibraryConfigByLevelIdAsync(string levelId)
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

            try
            {
                Query query = firestore.Collection(COLLECTION_LEVEL_LIBRARY_CONFIG)
                    .WhereEqualTo("levelId", levelId);
                QuerySnapshot snapshot = await query.GetSnapshotAsync();

                if (snapshot.Count > 0)
                {
                    DocumentSnapshot firstDoc = snapshot.Documents.FirstOrDefault();
                    if (firstDoc != null)
                    {
                        Dictionary<string, object> data = firstDoc.ToDictionary();
                        LevelLibraryConfigData config = ParseLevelLibraryConfig(data);
                        Debug.Log($"[{GetServiceName()}] Loaded LevelLibraryConfig for levelId: {levelId}");
                        return config;
                    }
                }

                Debug.LogWarning($"[{GetServiceName()}] No LevelLibraryConfig found for levelId: {levelId}");
                return null;
            }
            catch (Exception e)
            {
                Debug.LogError($"[{GetServiceName()}] Error loading LevelLibraryConfig by levelId: {e.Message}");
                return null;
            }
#endif
        }

        public async Task<LevelLibraryConfigData> LoadLevelLibraryConfigByTypeAsync(int type)
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

            try
            {
                Query query = firestore.Collection(COLLECTION_LEVEL_LIBRARY_CONFIG)
                    .WhereEqualTo("type", type);
                QuerySnapshot snapshot = await query.GetSnapshotAsync();

                if (snapshot.Count > 0)
                {
                    DocumentSnapshot firstDoc = snapshot.Documents.FirstOrDefault();
                    if (firstDoc != null)
                    {
                        Dictionary<string, object> data = firstDoc.ToDictionary();
                        LevelLibraryConfigData config = ParseLevelLibraryConfig(data);
                        Debug.Log($"[{GetServiceName()}] Loaded LevelLibraryConfig for type: {type}");
                        return config;
                    }
                }

                Debug.LogWarning($"[{GetServiceName()}] No LevelLibraryConfig found for type: {type}");
                return null;
            }
            catch (Exception e)
            {
                Debug.LogError($"[{GetServiceName()}] Error loading LevelLibraryConfig by type: {e.Message}");
                return null;
            }
#endif
        }

        public LevelListData GetCachedLevelList()
        {
            return cachedLevelList ?? new LevelListData();
        }

        public List<LevelLibraryConfigData> GetCachedLevelLibraryConfigs()
        {
            return cachedLevelLibraryConfigs ?? new List<LevelLibraryConfigData>();
        }

        public async Task<bool> InitializeCollectionsIfEmptyAsync()
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
                bool levelListSuccess = await InitializeLevelListCollectionIfEmptyAsync();
                bool levelLibrarySuccess = await InitializeLevelLibraryConfigCollectionIfEmptyAsync();
                return levelListSuccess && levelLibrarySuccess;
            }
            catch (Exception e)
            {
                Debug.LogError($"[{GetServiceName()}] Error initializing collections: {e.Message}");
                return false;
            }
#endif
        }

#if FIREBASE_FIRESTORE_AVAILABLE
        private async Task<bool> InitializeLevelListCollectionIfEmptyAsync()
        {
            try
            {
                DocumentReference docRef = firestore.Collection(COLLECTION_LEVEL_LIST).Document("main");
                DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();

                if (snapshot.Exists)
                {
                    Debug.Log($"[{GetServiceName()}] {COLLECTION_LEVEL_LIST}/main already exists, skipping");
                    return true;
                }

                LevelListData defaultLevelList = DefaultGameData.GetDefaultLevelList();
                Dictionary<string, object> levelListData = new Dictionary<string, object>
                {
                    { "levels", ConvertLevelListToFirestoreFormat(defaultLevelList) },
                    { "createdAt", Timestamp.GetCurrentTimestamp() },
                    { "updatedAt", Timestamp.GetCurrentTimestamp() }
                };

                await docRef.SetAsync(levelListData);
                Debug.Log($"[{GetServiceName()}] Created main document in {COLLECTION_LEVEL_LIST} with {defaultLevelList.levels.Count} default levels");
                return true;
            }
            catch (Exception e)
            {
                string errorMsg = e.Message;
                if (errorMsg.Contains("permission") || errorMsg.Contains("Permission"))
                {
                    Debug.LogError($"[{GetServiceName()}] Permission denied when initializing {COLLECTION_LEVEL_LIST}.");
                }
                else
                {
                    Debug.LogError($"[{GetServiceName()}] Error initializing {COLLECTION_LEVEL_LIST}: {errorMsg}");
                }
                return false;
            }
        }

        private async Task<bool> InitializeLevelLibraryConfigCollectionIfEmptyAsync()
        {
            try
            {
                QuerySnapshot existingSnapshot = await firestore.Collection(COLLECTION_LEVEL_LIBRARY_CONFIG)
                    .Limit(1)
                    .GetSnapshotAsync();
                
                if (existingSnapshot.Count > 0)
                {
                    Debug.Log($"[{GetServiceName()}] {COLLECTION_LEVEL_LIBRARY_CONFIG} is not empty, checking for updates...");
                }

                List<LevelLibraryConfigData> defaultConfigs = DefaultGameData.GetDefaultLevelLibraryConfigs();
                
                if (defaultConfigs == null || defaultConfigs.Count == 0)
                {
                    Debug.LogError($"[{GetServiceName()}] GetDefaultLevelLibraryConfigs returned empty list!");
                    return false;
                }
                
                QuerySnapshot fullSnapshot = await firestore.Collection(COLLECTION_LEVEL_LIBRARY_CONFIG)
                    .GetSnapshotAsync();
                
                HashSet<int> existingTypes = new HashSet<int>();
                foreach (DocumentSnapshot doc in fullSnapshot.Documents)
                {
                    if (doc.TryGetValue("type", out object typeObj) && typeObj != null)
                    {
                        try
                        {
                            int type = Convert.ToInt32(typeObj);
                            existingTypes.Add(type);
                        }
                        catch { }
                    }
                }
                
                int createdCount = 0;
                int updatedCount = 0;
                int skippedCount = 0;
                
                foreach (var config in defaultConfigs)
                {
                    if (!DefaultGameData.IsValidLevelLibraryType(config.type))
                    {
                        skippedCount++;
                        continue;
                    }
                    
                    string docId = config.type.ToString("D2");
                    DocumentReference docRef = firestore.Collection(COLLECTION_LEVEL_LIBRARY_CONFIG).Document(docId);
                    
                    Dictionary<string, object> data = new Dictionary<string, object>
                    {
                        { "type", config.type },
                        { "levelId", config.levelId ?? "" },
                        { "towerLibraryPrefabName", config.towerLibraryPrefabName ?? "" },
                        { "towerPrefabTypes", config.towerPrefabTypes ?? new List<int>() },
                        { "description", config.description ?? "" },
                        { "updatedAt", Timestamp.GetCurrentTimestamp() }
                    };
                    
                    if (!existingTypes.Contains(config.type))
                    {
                        data["createdAt"] = Timestamp.GetCurrentTimestamp();
                        await docRef.SetAsync(data);
                        createdCount++;
                    }
                    else
                    {
                        DocumentSnapshot existingDoc = await docRef.GetSnapshotAsync();
                        if (existingDoc.Exists)
                        {
                            Dictionary<string, object> existingData = existingDoc.ToDictionary();
                            if (!existingData.ContainsKey("towerPrefabTypes"))
                            {
                                Dictionary<string, object> updateData = new Dictionary<string, object>
                                {
                                    { "towerPrefabTypes", config.towerPrefabTypes ?? new List<int>() },
                                    { "updatedAt", Timestamp.GetCurrentTimestamp() }
                                };
                                await docRef.UpdateAsync(updateData);
                                updatedCount++;
                            }
                            else
                            {
                                skippedCount++;
                            }
                        }
                        else
                        {
                            skippedCount++;
                        }
                    }
                }
                
                Debug.Log($"[{GetServiceName()}] ✅ Initialized {COLLECTION_LEVEL_LIBRARY_CONFIG}: {createdCount} created, {updatedCount} updated, {skippedCount} skipped");
                return true;
            }
            catch (Exception e)
            {
                string errorMsg = e.Message;
                if (errorMsg.Contains("permission") || errorMsg.Contains("Permission"))
                {
                    Debug.LogError($"[{GetServiceName()}] Permission denied when initializing {COLLECTION_LEVEL_LIBRARY_CONFIG}.");
                }
                else
                {
                    Debug.LogError($"[{GetServiceName()}] Error initializing {COLLECTION_LEVEL_LIBRARY_CONFIG}: {errorMsg}");
                }
                return false;
            }
        }

        private LevelListData ParseLevelList(Dictionary<string, object> data)
        {
            try
            {
                LevelListData levelList = new LevelListData();
                
                if (data.ContainsKey("levels") && data["levels"] is List<object> levelsList)
                {
                    foreach (object levelObj in levelsList)
                    {
                        if (levelObj is Dictionary<string, object> levelData)
                        {
                            LevelItemData levelItem = ParseLevelItem(levelData);
                            if (levelItem != null)
                            {
                                levelList.levels.Add(levelItem);
                            }
                        }
                    }
                }

                return levelList;
            }
            catch (Exception e)
            {
                Debug.LogError($"[{GetServiceName()}] Error parsing LevelList: {e.Message}");
                return new LevelListData();
            }
        }

        private LevelItemData ParseLevelItem(Dictionary<string, object> data)
        {
            try
            {
                LevelItemData levelItem = new LevelItemData();
                
                if (data.ContainsKey("id") && data["id"] != null)
                {
                    levelItem.id = data["id"].ToString();
                }
                
                if (data.ContainsKey("name") && data["name"] != null)
                {
                    levelItem.name = data["name"].ToString();
                }
                
                if (data.ContainsKey("description") && data["description"] != null)
                {
                    levelItem.description = data["description"].ToString();
                }
                
                if (data.ContainsKey("sceneName") && data["sceneName"] != null)
                {
                    levelItem.sceneName = data["sceneName"].ToString();
                }

                return levelItem;
            }
            catch (Exception e)
            {
                Debug.LogError($"[{GetServiceName()}] Error parsing LevelItem: {e.Message}");
                return null;
            }
        }

        private LevelLibraryConfigData ParseLevelLibraryConfig(Dictionary<string, object> data)
        {
            try
            {
                LevelLibraryConfigData config = new LevelLibraryConfigData();
                
                if (data.ContainsKey("type") && data["type"] != null)
                {
                    config.type = Convert.ToInt32(data["type"]);
                }
                
                if (data.ContainsKey("levelId") && data["levelId"] != null)
                {
                    config.levelId = data["levelId"].ToString();
                }
                
                if (data.ContainsKey("towerLibraryPrefabName") && data["towerLibraryPrefabName"] != null)
                {
                    config.towerLibraryPrefabName = data["towerLibraryPrefabName"].ToString();
                }
                
                if (data.ContainsKey("towerPrefabTypes") && data["towerPrefabTypes"] != null)
                {
                    config.towerPrefabTypes = new List<int>();
                    if (data["towerPrefabTypes"] is List<object> towerTypesList)
                    {
                        foreach (object towerTypeObj in towerTypesList)
                        {
                            try
                            {
                                int towerType = Convert.ToInt32(towerTypeObj);
                                if (DefaultGameData.IsValidTowerPrefabType(towerType))
                                {
                                    config.towerPrefabTypes.Add(towerType);
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.LogWarning($"[{GetServiceName()}] Error parsing tower prefab type: {ex.Message}");
                            }
                        }
                    }
                }
                
                if (data.ContainsKey("description") && data["description"] != null)
                {
                    config.description = data["description"].ToString();
                }

                return config;
            }
            catch (Exception e)
            {
                Debug.LogError($"[{GetServiceName()}] Error parsing LevelLibraryConfig: {e.Message}");
                return null;
            }
        }

        private List<object> ConvertLevelListToFirestoreFormat(LevelListData levelList)
        {
            List<object> firestoreLevels = new List<object>();
            foreach (var level in levelList.levels)
            {
                Dictionary<string, object> levelData = new Dictionary<string, object>
                {
                    { "id", level.id ?? "" },
                    { "name", level.name ?? "" },
                    { "description", level.description ?? "" },
                    { "sceneName", level.sceneName ?? "" }
                };
                firestoreLevels.Add(levelData);
            }
            return firestoreLevels;
        }
#endif

        public override void Shutdown()
        {
            base.Shutdown();
            cachedLevelList = null;
            cachedLevelLibraryConfigs = null;
        }
    }
}

