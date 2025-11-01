using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Firebase;
#if FIREBASE_FIRESTORE_AVAILABLE
using Firebase.Firestore;
#endif
using Firebase.Extensions;
using Services.Core;
using Services.Data;
using Services.Managers;

namespace Services.Firestore
{
    /// <summary>
    /// Firebase Firestore implementation for loading configuration data
    /// Loads AgentConfiguration, TowerLevelData, and LevelList from Firestore collections
    /// 
    /// NOTE: Để sử dụng service này, bạn cần cài đặt Firebase Firestore package:
    /// 1. Tải Firebase Unity SDK từ: https://firebase.google.com/download/unity
    /// 2. Import package FirebaseFirestore.unitypackage
    /// 3. Xem chi tiết trong file FIREBASE_FIRESTORE_INSTALL.md
    /// 
    /// Sau khi cài đặt package, define symbol FIREBASE_FIRESTORE_AVAILABLE sẽ được tự động thêm vào.
    /// </summary>
    public class FirebaseFirestoreService : MonoBehaviour, IFirestoreService
    {
#if FIREBASE_FIRESTORE_AVAILABLE
        private FirebaseFirestore firestore;
#else
        private object firestore; // Placeholder khi package chưa được cài đặt
#endif
        private bool isInitialized = false;

        // Cached data
        private List<AgentConfigurationData> cachedAgentConfigurations;
        private List<TowerLevelDataData> cachedTowerLevelData;
        private LevelListData cachedLevelList;
        private List<LevelLibraryConfigData> cachedLevelLibraryConfigs;
        private bool isConfigDataLoaded = false;

        // Collection names - fallback defaults
        private const string DEFAULT_COLLECTION_AGENT_CONFIGURATIONS = "AgentConfigurations";
        private const string DEFAULT_COLLECTION_TOWER_LEVEL_DATA = "TowerLevelData";
        private const string DEFAULT_COLLECTION_LEVEL_LIST = "LevelList";
        private const string DEFAULT_COLLECTION_LEVEL_LIBRARY_CONFIG = "LevelLibraryConfig";
        private const string DEFAULT_COLLECTION_USERS = "users";

        // Properties to get collection names from config
        private string COLLECTION_AGENT_CONFIGURATIONS => 
            FirebaseConfigManager.Instance?.GetAgentConfigurationsCollection() ?? DEFAULT_COLLECTION_AGENT_CONFIGURATIONS;
        private string COLLECTION_TOWER_LEVEL_DATA => 
            FirebaseConfigManager.Instance?.GetTowerLevelDataCollection() ?? DEFAULT_COLLECTION_TOWER_LEVEL_DATA;
        private string COLLECTION_LEVEL_LIST => 
            FirebaseConfigManager.Instance?.GetLevelListCollection() ?? DEFAULT_COLLECTION_LEVEL_LIST;
        private string COLLECTION_LEVEL_LIBRARY_CONFIG => 
            FirebaseConfigManager.Instance?.GetLevelLibraryConfigCollection() ?? DEFAULT_COLLECTION_LEVEL_LIBRARY_CONFIG;
        private string COLLECTION_USERS => DEFAULT_COLLECTION_USERS;

        // Events
        public event Action<List<AgentConfigurationData>> OnAgentConfigurationsLoaded;
        public event Action<List<TowerLevelDataData>> OnTowerLevelDataLoaded;
        public event Action<LevelListData> OnLevelListLoaded;
        public event Action<List<LevelLibraryConfigData>> OnLevelLibraryConfigsLoaded;
        public event Action OnAllConfigDataLoaded;

        public bool IsInitialized => isInitialized;
        public bool IsConfigDataLoaded => isConfigDataLoaded;

        private void Awake()
        {
            // Ensure this persists across scenes
            DontDestroyOnLoad(gameObject);
        }

        public void Initialize()
        {
#if !FIREBASE_FIRESTORE_AVAILABLE
            Debug.LogError("[FirebaseFirestoreService] Firebase Firestore package chưa được cài đặt! " +
                "Vui lòng import package FirebaseFirestore.unitypackage. " +
                "Xem file FIREBASE_FIRESTORE_INSTALL.md để biết chi tiết.");
            return;
#else
            if (isInitialized)
            {
                Debug.LogWarning("[FirebaseFirestoreService] Already initialized");
                return;
            }

            // Use centralized Firebase initialization service
            FirebaseInitializationService firebaseInit = FirebaseInitializationService.Instance;
            
            if (firebaseInit.IsInitialized)
            {
                InitializeFirestoreInstance();
            }
            else
            {
                // Wait for Firebase to be initialized
                StartCoroutine(WaitForFirebaseInitialization());
            }
#endif
        }

#if FIREBASE_FIRESTORE_AVAILABLE
        private IEnumerator WaitForFirebaseInitialization()
        {
            FirebaseInitializationService firebaseInit = FirebaseInitializationService.Instance;
            
            // Check if already initialized
            if (firebaseInit.IsInitialized)
            {
                Debug.Log("[FirebaseFirestoreService] Firebase already initialized, initializing Firestore immediately...");
                InitializeFirestoreInstance();
                yield break;
            }
            
            // Initialize Firebase if not already initializing
            if (!firebaseInit.IsInitialized && !firebaseInit.IsInitializing)
            {
                firebaseInit.Initialize();
            }

            // Wait for initialization using event-based approach
            bool initCompleted = false;
            bool initSuccess = false;
            Action<bool> handler = null;
            
            handler = (success) =>
            {
                initCompleted = true;
                initSuccess = success;
                firebaseInit.OnInitializationComplete -= handler;
            };
            
            firebaseInit.OnInitializationComplete += handler;
            
            // Also start async task for timeout protection
            Task<bool> initTask = firebaseInit.WaitForInitializationAsync(30);
            
            // Wait until initialization completes
            float checkInterval = 0.1f;
            while (!initCompleted && !initTask.IsCompleted)
            {
                yield return new WaitForSeconds(checkInterval);
            }
            
            // Remove handler if not already removed
            firebaseInit.OnInitializationComplete -= handler;

            // Determine result
            bool success = false;
            if (initCompleted)
            {
                success = initSuccess;
            }
            else if (initTask.IsCompleted)
            {
                success = initTask.Result;
            }

            if (success && firebaseInit.DependencyStatus == DependencyStatus.Available)
            {
                InitializeFirestoreInstance();
            }
            else
            {
                Debug.LogError($"[FirebaseFirestoreService] Firebase initialization failed. Dependency status: {firebaseInit.DependencyStatus}");
            }
        }

        private void InitializeFirestoreInstance()
        {
            try
            {
                firestore = FirebaseFirestore.DefaultInstance;
                isInitialized = true;
                Debug.Log("[FirebaseFirestoreService] Initialized successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FirebaseFirestoreService] Failed to initialize FirebaseFirestore: {ex.Message}");
                Debug.LogError($"[FirebaseFirestoreService] Exception: {ex}");
            }
        }
#endif

        public void Shutdown()
        {
            isInitialized = false;
            firestore = null;
            ClearCache();
        }

        public async Task<List<AgentConfigurationData>> LoadAgentConfigurationsAsync()
        {
#if !FIREBASE_FIRESTORE_AVAILABLE
            Debug.LogError("[FirebaseFirestoreService] Firebase Firestore package chưa được cài đặt!");
            return new List<AgentConfigurationData>();
#else
            if (!isInitialized || firestore == null)
            {
                Debug.LogError("[FirebaseFirestoreService] Service not initialized");
                return new List<AgentConfigurationData>();
            }

            try
            {
                QuerySnapshot snapshot = await firestore.Collection(COLLECTION_AGENT_CONFIGURATIONS).GetSnapshotAsync();

                List<AgentConfigurationData> configurations = new List<AgentConfigurationData>();
                foreach (DocumentSnapshot document in snapshot.Documents)
                {
                    Dictionary<string, object> data = document.ToDictionary();
                    AgentConfigurationData config = ParseAgentConfiguration(data);
                    if (config != null)
                    {
                        // Only add if enum value matches client enum (safety check)
                        if (DefaultGameData.IsValidAgentType(config.type))
                        {
                            configurations.Add(config);
                        }
                        else
                        {
                            Debug.LogWarning($"[FirebaseFirestoreService] Skipping AgentConfiguration with invalid enum type: {config.type} (not in client AgentType enum)");
                        }
                    }
                }

                cachedAgentConfigurations = configurations;
                OnAgentConfigurationsLoaded?.Invoke(configurations);
                Debug.Log($"[FirebaseFirestoreService] Loaded {configurations.Count} AgentConfigurations");
                return configurations;
            }
            catch (Exception e)
            {
                string errorMsg = e.Message;
                if (errorMsg.Contains("permission") || errorMsg.Contains("Permission"))
                {
                    Debug.LogError($"[FirebaseFirestoreService] Permission denied when loading AgentConfigurations. " +
                                 $"Vui lòng cấu hình Firestore Rules trong Firebase Console. " +
                                 $"Xem hướng dẫn: FIRESTORE_RULES_SETUP.md");
                }
                else
                {
                    Debug.LogError($"[FirebaseFirestoreService] Error loading AgentConfigurations: {errorMsg}");
                }
                Debug.LogError($"[FirebaseFirestoreService] Full exception: {e}");
                return new List<AgentConfigurationData>();
            }
#endif
        }

        public async Task<List<AgentConfigurationData>> LoadAgentConfigurationsByTypeAsync(int type)
        {
#if !FIREBASE_FIRESTORE_AVAILABLE
            Debug.LogError("[FirebaseFirestoreService] Firebase Firestore package chưa được cài đặt!");
            return new List<AgentConfigurationData>();
#else
            if (!isInitialized || firestore == null)
            {
                Debug.LogError("[FirebaseFirestoreService] Service not initialized");
                return new List<AgentConfigurationData>();
            }

            try
            {
                Query query = firestore.Collection(COLLECTION_AGENT_CONFIGURATIONS)
                    .WhereEqualTo("type", type);
                QuerySnapshot snapshot = await query.GetSnapshotAsync();

                List<AgentConfigurationData> configurations = new List<AgentConfigurationData>();
                foreach (DocumentSnapshot document in snapshot.Documents)
                {
                    Dictionary<string, object> data = document.ToDictionary();
                    AgentConfigurationData config = ParseAgentConfiguration(data);
                    if (config != null)
                    {
                        // Only add if enum value matches client enum (safety check)
                        if (DefaultGameData.IsValidAgentType(config.type))
                        {
                            configurations.Add(config);
                        }
                        else
                        {
                            Debug.LogWarning($"[FirebaseFirestoreService] Skipping AgentConfiguration with invalid enum type: {config.type} (not in client AgentType enum)");
                        }
                    }
                }

                Debug.Log($"[FirebaseFirestoreService] Loaded {configurations.Count} AgentConfigurations with type {type}");
                return configurations;
            }
            catch (Exception e)
            {
                Debug.LogError($"[FirebaseFirestoreService] Error loading AgentConfigurations by type: {e.Message}");
                return new List<AgentConfigurationData>();
            }
#endif
        }

        public async Task<List<TowerLevelDataData>> LoadTowerLevelDataAsync()
        {
#if !FIREBASE_FIRESTORE_AVAILABLE
            Debug.LogError("[FirebaseFirestoreService] Firebase Firestore package chưa được cài đặt!");
            return new List<TowerLevelDataData>();
#else
            if (!isInitialized || firestore == null)
            {
                Debug.LogError("[FirebaseFirestoreService] Service not initialized");
                return new List<TowerLevelDataData>();
            }

            try
            {
                QuerySnapshot snapshot = await firestore.Collection(COLLECTION_TOWER_LEVEL_DATA).GetSnapshotAsync();

                Debug.Log($"[FirebaseFirestoreService] Loading TowerLevelData: Found {snapshot.Count} documents in collection");
                
                List<TowerLevelDataData> towerData = new List<TowerLevelDataData>();
                foreach (DocumentSnapshot document in snapshot.Documents)
                {
                    string docId = document.Id;
                    Debug.Log($"[FirebaseFirestoreService] Processing document ID: '{docId}' (length: {docId.Length})");
                    
                    Dictionary<string, object> data = document.ToDictionary();
                    
                    int typeFromField = -1;
                    int typeFromDocId = -1;
                    
                    // Get type from field (most reliable)
                    if (data.ContainsKey("type") && data["type"] != null)
                    {
                        try
                        {
                            typeFromField = Convert.ToInt32(data["type"]);
                            Debug.Log($"[FirebaseFirestoreService] Document '{docId}' has type field: {typeFromField} (type: {data["type"].GetType().Name})");
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning($"[FirebaseFirestoreService] Error parsing type field from document '{docId}': {ex.Message}");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[FirebaseFirestoreService] Document '{docId}' does NOT have 'type' field! Available fields: {string.Join(", ", data.Keys)}");
                    }
                    
                    // Infer type from document ID - support both padded ("00", "01") and unpadded ("0", "1") formats
                    if (int.TryParse(docId, out int inferredType))
                    {
                        typeFromDocId = inferredType;
                        Debug.Log($"[FirebaseFirestoreService] Inferred type {typeFromDocId} from document ID '{docId}'");
                        
                        // If type field is missing, use inferred type
                        if (typeFromField == -1)
                        {
                            data["type"] = typeFromDocId;
                            Debug.Log($"[FirebaseFirestoreService] Using inferred type {typeFromDocId} from document ID");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[FirebaseFirestoreService] Could not infer type from document ID '{docId}' (not a valid number)");
                    }
                    
                    // Validate: type from field should match type from document ID (if both exist)
                    if (typeFromField != -1 && typeFromDocId != -1 && typeFromField != typeFromDocId)
                    {
                        Debug.LogWarning($"[FirebaseFirestoreService] ⚠️ Type mismatch in document '{docId}': field type={typeFromField}, docId inferred type={typeFromDocId}. Using field type.");
                    }
                    
                    TowerLevelDataData tower = ParseTowerLevelData(data);
                    if (tower != null)
                    {
                        // Final validation: ensure type field was set (check if typeFromField or typeFromDocId was valid)
                        if (typeFromField == -1 && typeFromDocId == -1)
                        {
                            Debug.LogWarning($"[FirebaseFirestoreService] ⚠️ Document '{docId}' has no valid type (neither field nor docId provided valid type). Skipping.");
                            continue;
                        }
                        
                        // Validate expected document ID format matches type
                        string expectedPaddedDocId = tower.type.ToString("D2");
                        if (docId != expectedPaddedDocId && docId != tower.type.ToString())
                        {
                            Debug.LogWarning($"[FirebaseFirestoreService] ⚠️ Document ID '{docId}' doesn't match expected format for type {tower.type} (expected '{expectedPaddedDocId}' or '{tower.type}'). Still loading data.");
                        }
                        
                        Debug.Log($"[FirebaseFirestoreService] Parsed tower data: type={tower.type} ({GetTowerTypeName(tower.type)}), docId={docId}, expectedDocId={expectedPaddedDocId}");
                        
                        // Only add if enum value matches client enum (safety check)
                        if (DefaultGameData.IsValidTowerType(tower.type))
                        {
                            towerData.Add(tower);
                            Debug.Log($"[FirebaseFirestoreService] ✅ Added tower type {tower.type} ({GetTowerTypeName(tower.type)}) to loaded list");
                        }
                        else
                        {
                            Debug.LogWarning($"[FirebaseFirestoreService] Skipping TowerLevelData with invalid enum type: {tower.type} (not in client TowerType enum)");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[FirebaseFirestoreService] Failed to parse tower data from document '{docId}'");
                    }
                }

                cachedTowerLevelData = towerData;
                OnTowerLevelDataLoaded?.Invoke(towerData);
                Debug.Log($"[FirebaseFirestoreService] ✅ Loaded {towerData.Count} TowerLevelData documents (expected up to 16)");
                
                // Log all loaded types
                if (towerData.Count > 0)
                {
                    var loadedTypes = towerData.Select(t => t.type).OrderBy(t => t).ToList();
                    Debug.Log($"[FirebaseFirestoreService] Loaded tower types: {string.Join(", ", loadedTypes)}");
                }
                else
                {
                    Debug.LogWarning($"[FirebaseFirestoreService] ⚠️ No tower data loaded! Collection has {snapshot.Count} documents but none were parsed successfully.");
                }
                
                return towerData;
            }
            catch (Exception e)
            {
                string errorMsg = e.Message;
                if (errorMsg.Contains("permission") || errorMsg.Contains("Permission"))
                {
                    Debug.LogError($"[FirebaseFirestoreService] Permission denied when loading TowerLevelData. " +
                                 $"Vui lòng cấu hình Firestore Rules trong Firebase Console. " +
                                 $"Xem hướng dẫn: FIRESTORE_RULES_SETUP.md");
                }
                else
                {
                    Debug.LogError($"[FirebaseFirestoreService] Error loading TowerLevelData: {errorMsg}");
                }
                Debug.LogError($"[FirebaseFirestoreService] Full exception: {e}");
                return new List<TowerLevelDataData>();
            }
#endif
        }

        public async Task<List<TowerLevelDataData>> LoadTowerLevelDataByTypeAsync(int type)
        {
#if !FIREBASE_FIRESTORE_AVAILABLE
            Debug.LogError("[FirebaseFirestoreService] Firebase Firestore package chưa được cài đặt!");
            return new List<TowerLevelDataData>();
#else
            if (!isInitialized || firestore == null)
            {
                Debug.LogError("[FirebaseFirestoreService] Service not initialized");
                return new List<TowerLevelDataData>();
            }

            try
            {
                Query query = firestore.Collection(COLLECTION_TOWER_LEVEL_DATA)
                    .WhereEqualTo("type", type);
                QuerySnapshot snapshot = await query.GetSnapshotAsync();

                List<TowerLevelDataData> towerData = new List<TowerLevelDataData>();
                foreach (DocumentSnapshot document in snapshot.Documents)
                {
                    Dictionary<string, object> data = document.ToDictionary();
                    TowerLevelDataData tower = ParseTowerLevelData(data);
                    if (tower != null)
                    {
                        // Only add if enum value matches client enum (safety check)
                        if (DefaultGameData.IsValidTowerType(tower.type))
                        {
                            towerData.Add(tower);
                        }
                        else
                        {
                            Debug.LogWarning($"[FirebaseFirestoreService] Skipping TowerLevelData with invalid enum type: {tower.type} (not in client TowerType enum)");
                        }
                    }
                }

                Debug.Log($"[FirebaseFirestoreService] Loaded {towerData.Count} TowerLevelData with type {type}");
                return towerData;
            }
            catch (Exception e)
            {
                Debug.LogError($"[FirebaseFirestoreService] Error loading TowerLevelData by type: {e.Message}");
                return new List<TowerLevelDataData>();
            }
#endif
        }

        public async Task<LevelListData> LoadLevelListAsync()
        {
#if !FIREBASE_FIRESTORE_AVAILABLE
            Debug.LogError("[FirebaseFirestoreService] Firebase Firestore package chưa được cài đặt!");
            return new LevelListData();
#else
            if (!isInitialized || firestore == null)
            {
                Debug.LogError("[FirebaseFirestoreService] Service not initialized");
                return new LevelListData();
            }

            try
            {
                // Load from a single document containing the level list
                DocumentReference docRef = firestore.Collection(COLLECTION_LEVEL_LIST).Document("main");
                DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();

                if (snapshot.Exists)
                {
                    Dictionary<string, object> data = snapshot.ToDictionary();
                    LevelListData levelList = ParseLevelList(data);
                    cachedLevelList = levelList;
                    OnLevelListLoaded?.Invoke(levelList);
                    Debug.Log($"[FirebaseFirestoreService] Loaded LevelList with {levelList.levels.Count} levels");
                    return levelList;
                }
                else
                {
                    Debug.LogWarning("[FirebaseFirestoreService] LevelList document not found");
                    return new LevelListData();
                }
            }
            catch (Exception e)
            {
                string errorMsg = e.Message;
                if (errorMsg.Contains("permission") || errorMsg.Contains("Permission"))
                {
                    Debug.LogError($"[FirebaseFirestoreService] Permission denied when loading LevelList. " +
                                 $"Vui lòng cấu hình Firestore Rules trong Firebase Console. " +
                                 $"Xem hướng dẫn: FIRESTORE_RULES_SETUP.md");
                }
                else
                {
                    Debug.LogError($"[FirebaseFirestoreService] Error loading LevelList: {errorMsg}");
                }
                Debug.LogError($"[FirebaseFirestoreService] Full exception: {e}");
                return new LevelListData();
            }
#endif
        }

        public async Task LoadAllConfigDataAsync()
        {
#if !FIREBASE_FIRESTORE_AVAILABLE
            Debug.LogError("[FirebaseFirestoreService] Firebase Firestore package chưa được cài đặt!");
            await Task.CompletedTask;
#else
            if (!isInitialized)
            {
                Debug.LogError("[FirebaseFirestoreService] Service not initialized");
                return;
            }

            try
            {
                Debug.Log("[FirebaseFirestoreService] Starting to load all config data...");
                
                // Load all data in parallel
                Task<List<AgentConfigurationData>> agentTask = LoadAgentConfigurationsAsync();
                Task<List<TowerLevelDataData>> towerTask = LoadTowerLevelDataAsync();
                Task<LevelListData> levelTask = LoadLevelListAsync();
                Task<List<LevelLibraryConfigData>> levelLibraryTask = LoadLevelLibraryConfigsAsync();

                await Task.WhenAll(agentTask, towerTask, levelTask, levelLibraryTask);

                // Sync data vào ScriptableObjects sau khi load xong
                Debug.Log("[FirebaseFirestoreService] Syncing data to ScriptableObjects...");
                GameDataSyncService.SyncTowerLevelDataToScriptableObjects(cachedTowerLevelData);
                GameDataSyncService.SyncAgentConfigurationsToScriptableObjects(cachedAgentConfigurations);
                GameDataSyncService.SyncLevelListToScriptableObject(cachedLevelList);
                Debug.Log("[FirebaseFirestoreService] ScriptableObject sync completed");

                isConfigDataLoaded = true;
                OnAllConfigDataLoaded?.Invoke();
                Debug.Log("[FirebaseFirestoreService] All config data loaded and synced to ScriptableObjects successfully");
            }
            catch (Exception e)
            {
                Debug.LogError($"[FirebaseFirestoreService] Error loading all config data: {e.Message}");
            }
#endif
        }

        public List<AgentConfigurationData> GetCachedAgentConfigurations()
        {
            return cachedAgentConfigurations ?? new List<AgentConfigurationData>();
        }

        public List<TowerLevelDataData> GetCachedTowerLevelData()
        {
            return cachedTowerLevelData ?? new List<TowerLevelDataData>();
        }

        public LevelListData GetCachedLevelList()
        {
            return cachedLevelList ?? new LevelListData();
        }

        public List<LevelLibraryConfigData> GetCachedLevelLibraryConfigs()
        {
            return cachedLevelLibraryConfigs ?? new List<LevelLibraryConfigData>();
        }

        private AgentConfigurationData ParseAgentConfiguration(Dictionary<string, object> data)
        {
            try
            {
                AgentConfigurationData config = new AgentConfigurationData();
                
                if (data.ContainsKey("type") && data["type"] != null)
                {
                    config.type = Convert.ToInt32(data["type"]);
                }
                
                if (data.ContainsKey("agentName") && data["agentName"] != null)
                {
                    config.agentName = data["agentName"].ToString();
                }
                
                if (data.ContainsKey("agentDescription") && data["agentDescription"] != null)
                {
                    config.agentDescription = data["agentDescription"].ToString();
                }

                return config;
            }
            catch (Exception e)
            {
                Debug.LogError($"[FirebaseFirestoreService] Error parsing AgentConfiguration: {e.Message}");
                return null;
            }
        }

        private TowerLevelDataData ParseTowerLevelData(Dictionary<string, object> data)
        {
            try
            {
                TowerLevelDataData tower = new TowerLevelDataData();
                
                if (data.ContainsKey("type") && data["type"] != null)
                {
                    tower.type = Convert.ToInt32(data["type"]);
                }
                
                if (data.ContainsKey("description") && data["description"] != null)
                {
                    tower.description = data["description"].ToString();
                }
                
                if (data.ContainsKey("upgradeDescription") && data["upgradeDescription"] != null)
                {
                    tower.upgradeDescription = data["upgradeDescription"].ToString();
                }
                
                if (data.ContainsKey("cost") && data["cost"] != null)
                {
                    tower.cost = Convert.ToInt32(data["cost"]);
                }
                
                if (data.ContainsKey("sell") && data["sell"] != null)
                {
                    tower.sell = Convert.ToInt32(data["sell"]);
                }
                
                if (data.ContainsKey("maxHealth") && data["maxHealth"] != null)
                {
                    tower.maxHealth = Convert.ToInt32(data["maxHealth"]);
                }
                
                if (data.ContainsKey("startingHealth") && data["startingHealth"] != null)
                {
                    tower.startingHealth = Convert.ToInt32(data["startingHealth"]);
                }

                return tower;
            }
            catch (Exception e)
            {
                Debug.LogError($"[FirebaseFirestoreService] Error parsing TowerLevelData: {e.Message}");
                return null;
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
                Debug.LogError($"[FirebaseFirestoreService] Error parsing LevelList: {e.Message}");
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
                Debug.LogError($"[FirebaseFirestoreService] Error parsing LevelItem: {e.Message}");
                return null;
            }
        }

        public async Task<List<LevelLibraryConfigData>> LoadLevelLibraryConfigsAsync()
        {
#if !FIREBASE_FIRESTORE_AVAILABLE
            Debug.LogError("[FirebaseFirestoreService] Firebase Firestore package chưa được cài đặt!");
            return new List<LevelLibraryConfigData>();
#else
            if (!isInitialized || firestore == null)
            {
                Debug.LogError("[FirebaseFirestoreService] Service not initialized");
                return new List<LevelLibraryConfigData>();
            }

            try
            {
                QuerySnapshot snapshot = await firestore.Collection(COLLECTION_LEVEL_LIBRARY_CONFIG).GetSnapshotAsync();

                Debug.Log($"[FirebaseFirestoreService] Loading LevelLibraryConfig: Found {snapshot.Count} documents in collection");
                
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
                Debug.Log($"[FirebaseFirestoreService] Loaded {configs.Count} LevelLibraryConfigs");
                return configs;
            }
            catch (Exception e)
            {
                string errorMsg = e.Message;
                if (errorMsg.Contains("permission") || errorMsg.Contains("Permission"))
                {
                    Debug.LogError($"[FirebaseFirestoreService] Permission denied when loading LevelLibraryConfig. " +
                                 $"Vui lòng cấu hình Firestore Rules trong Firebase Console. " +
                                 $"Xem hướng dẫn: FIRESTORE_RULES_SETUP.md");
                }
                else
                {
                    Debug.LogError($"[FirebaseFirestoreService] Error loading LevelLibraryConfig: {errorMsg}");
                }
                Debug.LogError($"[FirebaseFirestoreService] Full exception: {e}");
                return new List<LevelLibraryConfigData>();
            }
#endif
        }

        public async Task<LevelLibraryConfigData> LoadLevelLibraryConfigByLevelIdAsync(string levelId)
        {
#if !FIREBASE_FIRESTORE_AVAILABLE
            Debug.LogError("[FirebaseFirestoreService] Firebase Firestore package chưa được cài đặt!");
            return null;
#else
            if (!isInitialized || firestore == null)
            {
                Debug.LogError("[FirebaseFirestoreService] Service not initialized");
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
                        Debug.Log($"[FirebaseFirestoreService] Loaded LevelLibraryConfig for levelId: {levelId}");
                        return config;
                    }
                }

                Debug.LogWarning($"[FirebaseFirestoreService] No LevelLibraryConfig found for levelId: {levelId}");
                return null;
            }
            catch (Exception e)
            {
                Debug.LogError($"[FirebaseFirestoreService] Error loading LevelLibraryConfig by levelId: {e.Message}");
                return null;
            }
#endif
        }

        public async Task<LevelLibraryConfigData> LoadLevelLibraryConfigByTypeAsync(int type)
        {
#if !FIREBASE_FIRESTORE_AVAILABLE
            Debug.LogError("[FirebaseFirestoreService] Firebase Firestore package chưa được cài đặt!");
            return null;
#else
            if (!isInitialized || firestore == null)
            {
                Debug.LogError("[FirebaseFirestoreService] Service not initialized");
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
                        Debug.Log($"[FirebaseFirestoreService] Loaded LevelLibraryConfig for type: {type}");
                        return config;
                    }
                }

                Debug.LogWarning($"[FirebaseFirestoreService] No LevelLibraryConfig found for type: {type}");
                return null;
            }
            catch (Exception e)
            {
                Debug.LogError($"[FirebaseFirestoreService] Error loading LevelLibraryConfig by type: {e.Message}");
                return null;
            }
#endif
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
                
                if (data.ContainsKey("description") && data["description"] != null)
                {
                    config.description = data["description"].ToString();
                }

                return config;
            }
            catch (Exception e)
            {
                Debug.LogError($"[FirebaseFirestoreService] Error parsing LevelLibraryConfig: {e.Message}");
                return null;
            }
        }

        public async Task<bool> SaveUserDataAsync(UserInfo userInfo)
        {
#if !FIREBASE_FIRESTORE_AVAILABLE
            Debug.LogError("[FirebaseFirestoreService] Firebase Firestore package chưa được cài đặt!");
            return false;
#else
            if (!isInitialized || firestore == null)
            {
                Debug.LogError("[FirebaseFirestoreService] Service not initialized");
                return false;
            }

            if (userInfo == null || string.IsNullOrEmpty(userInfo.UID))
            {
                Debug.LogError("[FirebaseFirestoreService] Invalid user info: UID is required");
                return false;
            }

            try
            {
                // Get reference to user document (using UID as document ID)
                DocumentReference userDocRef = firestore.Collection(COLLECTION_USERS).Document(userInfo.UID);

                // Create dictionary with user data
                Dictionary<string, object> userData = new Dictionary<string, object>
                {
                    { "uid", userInfo.UID },
                    { "email", userInfo.Email ?? "" },
                    { "displayName", userInfo.DisplayName ?? "" },
                    { "photoURL", userInfo.PhotoURL ?? "" },
                    { "providerId", userInfo.ProviderId ?? "" },
                    { "lastLoginAt", Timestamp.GetCurrentTimestamp() },
                    { "updatedAt", Timestamp.GetCurrentTimestamp() }
                };

                // Check if document exists
                DocumentSnapshot existingDoc = await userDocRef.GetSnapshotAsync();
                
                if (existingDoc.Exists)
                {
                    // Update existing document (merge to keep existing fields)
                    Dictionary<string, object> updateData = new Dictionary<string, object>
                    {
                        { "email", userInfo.Email ?? "" },
                        { "displayName", userInfo.DisplayName ?? "" },
                        { "photoURL", userInfo.PhotoURL ?? "" },
                        { "providerId", userInfo.ProviderId ?? "" },
                        { "lastLoginAt", Timestamp.GetCurrentTimestamp() },
                        { "updatedAt", Timestamp.GetCurrentTimestamp() }
                    };

                    await userDocRef.UpdateAsync(updateData);
                    Debug.Log($"[FirebaseFirestoreService] Updated user data for UID: {userInfo.UID}");
                }
                else
                {
                    // Create new document
                    userData["createdAt"] = Timestamp.GetCurrentTimestamp();
                    await userDocRef.SetAsync(userData);
                    Debug.Log($"[FirebaseFirestoreService] Created user data for UID: {userInfo.UID}");
                }

                return true;
            }
            catch (Exception e)
            {
                string errorMsg = e.Message;
                if (errorMsg.Contains("permission") || errorMsg.Contains("Permission"))
                {
                    Debug.LogError($"[FirebaseFirestoreService] Permission denied when saving user data. " +
                                 $"Vui lòng cấu hình Firestore Rules trong Firebase Console. " +
                                 $"Xem hướng dẫn: FIRESTORE_RULES_SETUP.md");
                }
                else
                {
                    Debug.LogError($"[FirebaseFirestoreService] Error saving user data: {errorMsg}");
                }
                Debug.LogError($"[FirebaseFirestoreService] Full exception: {e}");
                return false;
            }
#endif
        }

        /// <summary>
        /// Check if collections are empty (no documents exist)
        /// Returns true only if ALL collections are completely empty
        /// </summary>
        public async Task<bool> AreCollectionsEmptyAsync()
        {
#if !FIREBASE_FIRESTORE_AVAILABLE
            return false;
#else
            if (!isInitialized || firestore == null)
            {
                return false;
            }

            try
            {
                // Check AgentConfigurations collection
                QuerySnapshot agentSnapshot = await firestore.Collection(COLLECTION_AGENT_CONFIGURATIONS)
                    .Limit(1)
                    .GetSnapshotAsync();
                if (agentSnapshot.Count > 0)
                {
                    Debug.Log("[FirebaseFirestoreService] AgentConfigurations collection is not empty, skipping initialization");
                    return false;
                }

                // Check TowerLevelData collection
                QuerySnapshot towerSnapshot = await firestore.Collection(COLLECTION_TOWER_LEVEL_DATA)
                    .Limit(1)
                    .GetSnapshotAsync();
                if (towerSnapshot.Count > 0)
                {
                    Debug.Log("[FirebaseFirestoreService] TowerLevelData collection is not empty, skipping initialization");
                    return false;
                }

                // Check LevelList collection
                QuerySnapshot levelSnapshot = await firestore.Collection(COLLECTION_LEVEL_LIST)
                    .Limit(1)
                    .GetSnapshotAsync();
                if (levelSnapshot.Count > 0)
                {
                    Debug.Log("[FirebaseFirestoreService] LevelList collection is not empty, skipping initialization");
                    return false;
                }

                Debug.Log("[FirebaseFirestoreService] All collections are empty, initialization needed");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[FirebaseFirestoreService] Error checking if collections are empty: {e.Message}");
                return false;
            }
#endif
        }

        public async Task<bool> InitializeCollectionsIfEmptyAsync()
        {
#if !FIREBASE_FIRESTORE_AVAILABLE
            Debug.LogError("[FirebaseFirestoreService] Firebase Firestore package chưa được cài đặt!");
            return false;
#else
            if (!isInitialized || firestore == null)
            {
                Debug.LogError("[FirebaseFirestoreService] Service not initialized");
                return false;
            }

            try
            {
                // ✅ QUAN TRỌNG: Chỉ initialize khi collections hoàn toàn empty
                // Không tạo/update nếu collections đã có data
                bool areEmpty = await AreCollectionsEmptyAsync();
                if (!areEmpty)
                {
                    Debug.Log("[FirebaseFirestoreService] Collections already have data, skipping initialization to preserve existing data");
                    return true; // Return true vì không có lỗi, chỉ skip initialization
                }

                Debug.Log("[FirebaseFirestoreService] All collections are empty, proceeding with initialization...");

                bool allSuccess = true;

                // Initialize AgentConfigurations collection (chỉ tạo, không update)
                bool agentConfigSuccess = await InitializeAgentConfigurationsCollectionAsync();
                if (!agentConfigSuccess)
                {
                    Debug.LogWarning("[FirebaseFirestoreService] Failed to initialize AgentConfigurations collection");
                    allSuccess = false;
                }

                // Initialize TowerLevelData collection (chỉ tạo, không update)
                bool towerLevelSuccess = await InitializeTowerLevelDataCollectionAsync();
                if (!towerLevelSuccess)
                {
                    Debug.LogWarning("[FirebaseFirestoreService] Failed to initialize TowerLevelData collection");
                    allSuccess = false;
                }

                // Initialize LevelList collection (chỉ tạo, không update)
                bool levelListSuccess = await InitializeLevelListCollectionAsync();
                if (!levelListSuccess)
                {
                    Debug.LogWarning("[FirebaseFirestoreService] Failed to initialize LevelList collection");
                    allSuccess = false;
                }

                if (allSuccess)
                {
                    Debug.Log("[FirebaseFirestoreService] All collections initialized successfully");
                }

                return allSuccess;
            }
            catch (Exception e)
            {
                Debug.LogError($"[FirebaseFirestoreService] Error initializing collections: {e.Message}");
                Debug.LogError($"[FirebaseFirestoreService] Full exception: {e}");
                return false;
            }
#endif
        }

#if FIREBASE_FIRESTORE_AVAILABLE
        /// <summary>
        /// Initialize AgentConfigurations collection with default data for all enum values
        /// Creates documents for all AgentType enum values with balanced default values
        /// </summary>
        private async Task<bool> InitializeAgentConfigurationsCollectionAsync()
        {
            try
            {
                // Get default configurations for all enum values
                List<AgentConfigurationData> defaultConfigs = DefaultGameData.GetDefaultAgentConfigurations();
                
                if (defaultConfigs == null || defaultConfigs.Count == 0)
                {
                    Debug.LogError("[FirebaseFirestoreService] GetDefaultAgentConfigurations returned empty list!");
                    return false;
                }
                
                Debug.Log($"[FirebaseFirestoreService] Preparing to initialize {defaultConfigs.Count} agent types");
                
                // Check existing documents
                QuerySnapshot existingSnapshot = await firestore.Collection(COLLECTION_AGENT_CONFIGURATIONS)
                    .GetSnapshotAsync();
                
                HashSet<int> existingTypes = new HashSet<int>();
                foreach (DocumentSnapshot doc in existingSnapshot.Documents)
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
                
                Debug.Log($"[FirebaseFirestoreService] Found {existingTypes.Count} existing agent type documents in Firestore");
                
                int createdCount = 0;
                int skippedCount = 0;
                
                // Create documents for all enum values (skip if already exist to preserve existing data)
                foreach (var config in defaultConfigs)
                {
                    // Validate enum value exists in client
                    if (!DefaultGameData.IsValidAgentType(config.type))
                    {
                        Debug.LogWarning($"[FirebaseFirestoreService] Skipping invalid AgentType: {config.type}");
                        skippedCount++;
                        continue;
                    }
                    
                    // Use padded document ID (2 digits) to ensure correct sorting: "00", "01", ..., "07"
                    string docId = config.type.ToString("D2"); // "D2" = decimal format with 2 digits padding
                    DocumentReference docRef = firestore.Collection(COLLECTION_AGENT_CONFIGURATIONS).Document(docId);
                    
                    Dictionary<string, object> data = new Dictionary<string, object>
                    {
                        { "type", config.type },
                        { "agentName", config.agentName ?? "" },
                        { "agentDescription", config.agentDescription ?? "" },
                        { "updatedAt", Timestamp.GetCurrentTimestamp() }
                    };
                    
                    if (!existingTypes.Contains(config.type))
                    {
                        // Create new document
                        data["createdAt"] = Timestamp.GetCurrentTimestamp();
                        await docRef.SetAsync(data);
                        createdCount++;
                        Debug.Log($"[FirebaseFirestoreService] Created agent type {config.type} ({docId}) in Firestore");
                    }
                    else
                    {
                        // ✅ QUAN TRỌNG: Không update document đã tồn tại để preserve data từ backend
                        // Chỉ log để debug
                        skippedCount++;
                        Debug.Log($"[FirebaseFirestoreService] Agent type {config.type} ({docId}) already exists, skipping to preserve existing data");
                    }
                }
                
                Debug.Log($"[FirebaseFirestoreService] ✅ Initialized {COLLECTION_AGENT_CONFIGURATIONS}: {createdCount} created, {skippedCount} skipped (total: {defaultConfigs.Count})");
                return true;
            }
            catch (Exception e)
            {
                string errorMsg = e.Message;
                if (errorMsg.Contains("permission") || errorMsg.Contains("Permission"))
                {
                    Debug.LogError($"[FirebaseFirestoreService] Permission denied when initializing {COLLECTION_AGENT_CONFIGURATIONS}. " +
                                 $"Vui lòng cấu hình Firestore Rules trong Firebase Console. " +
                                 $"Xem hướng dẫn: FIRESTORE_RULES_SETUP.md");
                }
                else
                {
                    Debug.LogError($"[FirebaseFirestoreService] Error initializing {COLLECTION_AGENT_CONFIGURATIONS}: {errorMsg}");
                }
                return false;
            }
        }

        /// <summary>
        /// Initialize TowerLevelData collection with default data for all enum values
        /// Creates documents for all TowerType enum values with balanced game values
        /// </summary>
        private async Task<bool> InitializeTowerLevelDataCollectionAsync()
        {
            try
            {
                // Get default tower data for all enum values
                List<TowerLevelDataData> defaultTowerData = DefaultGameData.GetDefaultTowerLevelData();
                
                if (defaultTowerData == null || defaultTowerData.Count == 0)
                {
                    Debug.LogError("[FirebaseFirestoreService] GetDefaultTowerLevelData returned empty list!");
                    return false;
                }
                
                Debug.Log($"[FirebaseFirestoreService] Preparing to initialize {defaultTowerData.Count} tower types");
                
                // Check existing documents - check both by document ID and by type field
                QuerySnapshot existingSnapshot = await firestore.Collection(COLLECTION_TOWER_LEVEL_DATA)
                    .GetSnapshotAsync();
                
                HashSet<int> existingTypes = new HashSet<int>();
                foreach (DocumentSnapshot doc in existingSnapshot.Documents)
                {
                    // Check by type field (most reliable)
                    if (doc.TryGetValue("type", out object typeObj) && typeObj != null)
                    {
                        try
                        {
                            int type = Convert.ToInt32(typeObj);
                            existingTypes.Add(type);
                            Debug.Log($"[FirebaseFirestoreService] Found existing tower type {type} (docId: {doc.Id})");
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning($"[FirebaseFirestoreService] Error parsing type from document {doc.Id}: {ex.Message}");
                        }
                    }
                    
                    // Also check by document ID - support both padded ("00", "01") and unpadded ("0", "1") formats
                    string docId = doc.Id;
                    if (int.TryParse(docId, out int docIdAsType))
                    {
                        existingTypes.Add(docIdAsType);
                        // If document ID is unpadded (e.g., "1", "2"), we'll migrate to padded format later
                    }
                }
                
                Debug.Log($"[FirebaseFirestoreService] Found {existingTypes.Count} existing tower type documents in Firestore (types: {string.Join(", ", existingTypes)})");
                
                int createdCount = 0;
                int skippedCount = 0;
                
                // Create or update documents for all enum values - ALWAYS ensure all 16 types exist
                Debug.Log($"[FirebaseFirestoreService] Processing {defaultTowerData.Count} tower types from enum...");
                Debug.Log($"[FirebaseFirestoreService] Expected 16 tower types (0-15), found {existingTypes.Count} existing types");
                
                foreach (var towerData in defaultTowerData)
                {
                    // Validate enum value exists in client
                    if (!DefaultGameData.IsValidTowerType(towerData.type))
                    {
                        Debug.LogWarning($"[FirebaseFirestoreService] Skipping invalid TowerType: {towerData.type}");
                        skippedCount++;
                        continue;
                    }
                    
                    // Use padded document ID (2 digits) to ensure correct sorting: "00", "01", ..., "15"
                    string docId = towerData.type.ToString("D2"); // "D2" = decimal format with 2 digits padding
                    DocumentReference docRef = firestore.Collection(COLLECTION_TOWER_LEVEL_DATA).Document(docId);
                    
                    bool exists = existingTypes.Contains(towerData.type);
                    Debug.Log($"[FirebaseFirestoreService] Processing TowerType {towerData.type} ({GetTowerTypeName(towerData.type)}) - docId: {docId}, exists: {exists}");
                    
                    Dictionary<string, object> data = new Dictionary<string, object>
                    {
                        { "type", towerData.type },
                        { "description", towerData.description ?? "" },
                        { "upgradeDescription", towerData.upgradeDescription ?? "" },
                        { "cost", towerData.cost },
                        { "sell", towerData.sell },
                        { "maxHealth", towerData.maxHealth },
                        { "startingHealth", towerData.startingHealth },
                        { "updatedAt", Timestamp.GetCurrentTimestamp() }
                    };
                    
                    try
                    {
                        // Check if document exists with unpadded ID (for migration)
                        string unpaddedDocId = towerData.type.ToString();
                        DocumentReference unpaddedDocRef = firestore.Collection(COLLECTION_TOWER_LEVEL_DATA).Document(unpaddedDocId);
                        DocumentSnapshot unpaddedDocSnapshot = await unpaddedDocRef.GetSnapshotAsync();
                        bool hasUnpaddedVersion = unpaddedDocSnapshot.Exists;
                        
                        if (!exists && !hasUnpaddedVersion)
                        {
                            // Create new document with padded ID
                            data["createdAt"] = Timestamp.GetCurrentTimestamp();
                            await docRef.SetAsync(data);
                            createdCount++;
                            Debug.Log($"[FirebaseFirestoreService] ✅ Created tower type {towerData.type} ({GetTowerTypeName(towerData.type)}) - docId: {docId} (padded)");
                        }
                        else
                        {
                            // Document exists - update or migrate to padded format
                            if (hasUnpaddedVersion && !exists)
                            {
                                // Migrate from unpadded to padded format
                                Debug.Log($"[FirebaseFirestoreService] Migrating tower type {towerData.type} from unpadded ID '{unpaddedDocId}' to padded ID '{docId}'");
                                
                                // Try to preserve createdAt from old document
                                if (unpaddedDocSnapshot.TryGetValue("createdAt", out object createdAtObj) && createdAtObj != null)
                                {
                                    data["createdAt"] = createdAtObj;
                                }
                                else
                                {
                                    data["createdAt"] = Timestamp.GetCurrentTimestamp();
                                }
                                
                                await docRef.SetAsync(data); // Create new document with padded ID
                                await unpaddedDocRef.DeleteAsync(); // Delete old unpadded document
                                createdCount++;
                                Debug.Log($"[FirebaseFirestoreService] ✅ Migrated tower type {towerData.type} to padded format - docId: {docId}");
                            }
                            else
                            {
                                // ✅ QUAN TRỌNG: Không update document đã tồn tại để preserve data từ backend
                                // Chỉ log để debug
                                skippedCount++;
                                Debug.Log($"[FirebaseFirestoreService] Tower type {towerData.type} ({GetTowerTypeName(towerData.type)}) - docId: {docId} already exists, skipping to preserve existing data");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[FirebaseFirestoreService] ❌ Error processing tower type {towerData.type} ({GetTowerTypeName(towerData.type)}): {ex.Message}");
                        skippedCount++;
                    }
                }
                
                // Log summary with all types processed
                List<int> allProcessedTypes = defaultTowerData.Select(t => t.type).ToList();
                List<int> createdTypes = new List<int>();
                List<int> skippedTypes = new List<int>();
                
                foreach (var towerData in defaultTowerData)
                {
                    if (existingTypes.Contains(towerData.type))
                        skippedTypes.Add(towerData.type); // Documents đã tồn tại được skip để preserve data
                    else if (DefaultGameData.IsValidTowerType(towerData.type))
                        createdTypes.Add(towerData.type);
                }
                
                // Cleanup: Delete all unpadded documents (single digit IDs like "0", "1", "2", ..., "9") 
                // after migration to padded format is complete
                // Re-query collection to get all current documents (including newly created ones)
                Debug.Log($"[FirebaseFirestoreService] Cleaning up unpadded documents...");
                QuerySnapshot allDocsSnapshot = await firestore.Collection(COLLECTION_TOWER_LEVEL_DATA).GetSnapshotAsync();
                int deletedUnpaddedCount = 0;
                
                foreach (DocumentSnapshot doc in allDocsSnapshot.Documents)
                {
                    string docId = doc.Id;
                    // Check if document ID is a single digit (unpadded format like "0", "1", ..., "9")
                    // or a single "0" that should be "00"
                    if ((docId.Length == 1 && char.IsDigit(docId[0])) || docId == "0")
                    {
                        try
                        {
                            // Check if corresponding padded document exists
                            int typeValue = int.Parse(docId);
                            string paddedDocId = typeValue.ToString("D2");
                            DocumentReference paddedDocRef = firestore.Collection(COLLECTION_TOWER_LEVEL_DATA).Document(paddedDocId);
                            DocumentSnapshot paddedDocSnapshot = await paddedDocRef.GetSnapshotAsync();
                            
                            if (paddedDocSnapshot.Exists)
                            {
                                // Padded version exists, safe to delete unpadded version
                                await doc.Reference.DeleteAsync();
                                deletedUnpaddedCount++;
                                Debug.Log($"[FirebaseFirestoreService] 🗑️ Deleted unpadded document '{docId}' (padded version '{paddedDocId}' exists)");
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning($"[FirebaseFirestoreService] Error deleting unpadded document '{docId}': {ex.Message}");
                        }
                    }
                }
                
                Debug.Log($"[FirebaseFirestoreService] ✅ Initialized {COLLECTION_TOWER_LEVEL_DATA}: {createdCount} created, {skippedCount} skipped (preserved existing data), {deletedUnpaddedCount} unpadded documents deleted (total: {defaultTowerData.Count})");
                Debug.Log($"[FirebaseFirestoreService] Created types: {string.Join(", ", createdTypes)}");
                Debug.Log($"[FirebaseFirestoreService] Skipped types (existing data preserved): {string.Join(", ", skippedTypes)}");
                Debug.Log($"[FirebaseFirestoreService] All processed types (0-15): {string.Join(", ", allProcessedTypes)}");
                
                if (createdCount + skippedCount < 16)
                {
                    Debug.LogWarning($"[FirebaseFirestoreService] ⚠️ Only {createdCount + skippedCount}/16 tower types were processed. Expected 16 types from enum.");
                }
                
                return true;
            }
            catch (Exception e)
            {
                string errorMsg = e.Message;
                if (errorMsg.Contains("permission") || errorMsg.Contains("Permission"))
                {
                    Debug.LogError($"[FirebaseFirestoreService] Permission denied when initializing {COLLECTION_TOWER_LEVEL_DATA}. " +
                                 $"Vui lòng cấu hình Firestore Rules trong Firebase Console. " +
                                 $"Xem hướng dẫn: FIRESTORE_RULES_SETUP.md");
                }
                else
                {
                    Debug.LogError($"[FirebaseFirestoreService] Error initializing {COLLECTION_TOWER_LEVEL_DATA}: {errorMsg}");
                }
                return false;
            }
        }

        /// <summary>
        /// Initialize LevelList collection with default balanced level list
        /// Creates default levels for game progression
        /// </summary>
        private async Task<bool> InitializeLevelListCollectionAsync()
        {
            try
            {
                // Check if main document exists
                DocumentReference docRef = firestore.Collection(COLLECTION_LEVEL_LIST).Document("main");
                DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();

                // Get default level list
                LevelListData defaultLevelList = DefaultGameData.GetDefaultLevelList();

                if (!snapshot.Exists)
                {
                    // Document doesn't exist, create with default levels
                    Dictionary<string, object> levelListData = new Dictionary<string, object>
                    {
                        { "levels", ConvertLevelListToFirestoreFormat(defaultLevelList) },
                        { "createdAt", Timestamp.GetCurrentTimestamp() },
                        { "updatedAt", Timestamp.GetCurrentTimestamp() }
                    };

                    await docRef.SetAsync(levelListData);

                    Debug.Log($"[FirebaseFirestoreService] Created main document in {COLLECTION_LEVEL_LIST} with {defaultLevelList.levels.Count} default levels");
                    return true;
                }
                else
                {
                    // ✅ QUAN TRỌNG: Document đã tồn tại, không update để preserve data từ backend
                    Dictionary<string, object> existingData = snapshot.ToDictionary();
                    if (existingData.ContainsKey("levels") && existingData["levels"] is List<object> existingLevels)
                    {
                        Debug.Log($"[FirebaseFirestoreService] {COLLECTION_LEVEL_LIST}/main already exists with {existingLevels.Count} levels, skipping to preserve existing data");
                    }
                    else
                    {
                        Debug.Log($"[FirebaseFirestoreService] {COLLECTION_LEVEL_LIST}/main already exists, skipping to preserve existing data");
                    }
                    return true;
                }
            }
            catch (Exception e)
            {
                string errorMsg = e.Message;
                if (errorMsg.Contains("permission") || errorMsg.Contains("Permission"))
                {
                    Debug.LogError($"[FirebaseFirestoreService] Permission denied when initializing {COLLECTION_LEVEL_LIST}. " +
                                 $"Vui lòng cấu hình Firestore Rules trong Firebase Console. " +
                                 $"Xem hướng dẫn: FIRESTORE_RULES_SETUP.md");
                }
                else
                {
                    Debug.LogError($"[FirebaseFirestoreService] Error initializing {COLLECTION_LEVEL_LIST}: {errorMsg}");
                }
                return false;
            }
        }

        /// <summary>
        /// Get tower type name for logging
        /// </summary>
        private string GetTowerTypeName(int type)
        {
            try
            {
                TowerDefense.Towers.Data.TowerType towerType = (TowerDefense.Towers.Data.TowerType)type;
                return towerType.ToString();
            }
            catch
            {
                return $"Unknown({type})";
            }
        }

        /// <summary>
        /// Convert LevelListData to Firestore format (List<Dictionary<string, object>>)
        /// </summary>
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

        private void ClearCache()
        {
            cachedAgentConfigurations = null;
            cachedTowerLevelData = null;
            cachedLevelList = null;
            cachedLevelLibraryConfigs = null;
            isConfigDataLoaded = false;
        }

        private void OnDestroy()
        {
            Shutdown();
        }
    }
}

