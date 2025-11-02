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
    /// Microservice for managing Tower Level Data
    /// Handles only Tower Data domain operations
    /// </summary>
    public class TowerDataService : FirestoreServiceBase, ITowerDataService
    {
        private const string DEFAULT_COLLECTION_TOWER_LEVEL_DATA = "TowerLevelData";
        private string COLLECTION_TOWER_LEVEL_DATA => 
            FirebaseConfigManager.Instance?.GetTowerLevelDataCollection() ?? DEFAULT_COLLECTION_TOWER_LEVEL_DATA;

        private List<TowerLevelDataData> cachedTowerLevelData;

        public event Action<List<TowerLevelDataData>> OnTowerLevelDataLoaded;

        protected override string GetServiceName() => "TowerDataService";

        public async Task<List<TowerLevelDataData>> LoadTowerLevelDataAsync()
        {
#if !FIREBASE_FIRESTORE_AVAILABLE
            Debug.LogError($"[{GetServiceName()}] Firebase Firestore package chưa được cài đặt!");
            return new List<TowerLevelDataData>();
#else
            if (!isInitialized || firestore == null)
            {
                Debug.LogError($"[{GetServiceName()}] Service not initialized");
                return new List<TowerLevelDataData>();
            }

            try
            {
                QuerySnapshot snapshot = await firestore.Collection(COLLECTION_TOWER_LEVEL_DATA).GetSnapshotAsync();

                Debug.Log($"[{GetServiceName()}] Loading TowerLevelData: Found {snapshot.Count} documents in collection");
                
                List<TowerLevelDataData> towerData = new List<TowerLevelDataData>();
                foreach (DocumentSnapshot document in snapshot.Documents)
                {
                    string docId = document.Id;
                    Dictionary<string, object> data = document.ToDictionary();
                    
                    int typeFromField = -1;
                    int typeFromDocId = -1;
                    
                    // Get type from field (most reliable)
                    if (data.ContainsKey("type") && data["type"] != null)
                    {
                        try
                        {
                            typeFromField = Convert.ToInt32(data["type"]);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning($"[{GetServiceName()}] Error parsing type field from document '{docId}': {ex.Message}");
                        }
                    }
                    
                    // Infer type from document ID - support both padded ("00", "01") and unpadded ("0", "1") formats
                    if (int.TryParse(docId, out int inferredType))
                    {
                        typeFromDocId = inferredType;
                        // If type field is missing, use inferred type
                        if (typeFromField == -1)
                        {
                            data["type"] = typeFromDocId;
                        }
                    }
                    
                    TowerLevelDataData tower = ParseTowerLevelData(data);
                    if (tower != null)
                    {
                        // Final validation: ensure type field was set
                        if (typeFromField == -1 && typeFromDocId == -1)
                        {
                            Debug.LogWarning($"[{GetServiceName()}] Document '{docId}' has no valid type. Skipping.");
                            continue;
                        }
                        
                        // Only add if enum value matches client enum (safety check)
                        if (DefaultGameData.IsValidTowerType(tower.type))
                        {
                            towerData.Add(tower);
                        }
                        else
                        {
                            Debug.LogWarning($"[{GetServiceName()}] Skipping TowerLevelData with invalid enum type: {tower.type}");
                        }
                    }
                }

                cachedTowerLevelData = towerData;
                OnTowerLevelDataLoaded?.Invoke(towerData);
                Debug.Log($"[{GetServiceName()}] ✅ Loaded {towerData.Count} TowerLevelData documents");
                return towerData;
            }
            catch (Exception e)
            {
                string errorMsg = e.Message;
                if (errorMsg.Contains("permission") || errorMsg.Contains("Permission"))
                {
                    Debug.LogError($"[{GetServiceName()}] Permission denied when loading TowerLevelData. " +
                                 $"Vui lòng cấu hình Firestore Rules trong Firebase Console.");
                }
                else
                {
                    Debug.LogError($"[{GetServiceName()}] Error loading TowerLevelData: {errorMsg}");
                }
                return new List<TowerLevelDataData>();
            }
#endif
        }

        public async Task<List<TowerLevelDataData>> LoadTowerLevelDataByTypeAsync(int type)
        {
#if !FIREBASE_FIRESTORE_AVAILABLE
            Debug.LogError($"[{GetServiceName()}] Firebase Firestore package chưa được cài đặt!");
            return new List<TowerLevelDataData>();
#else
            if (!isInitialized || firestore == null)
            {
                Debug.LogError($"[{GetServiceName()}] Service not initialized");
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
                        if (DefaultGameData.IsValidTowerType(tower.type))
                        {
                            towerData.Add(tower);
                        }
                    }
                }

                Debug.Log($"[{GetServiceName()}] Loaded {towerData.Count} TowerLevelData with type {type}");
                return towerData;
            }
            catch (Exception e)
            {
                Debug.LogError($"[{GetServiceName()}] Error loading TowerLevelData by type: {e.Message}");
                return new List<TowerLevelDataData>();
            }
#endif
        }

        public List<TowerLevelDataData> GetCachedTowerLevelData()
        {
            return cachedTowerLevelData ?? new List<TowerLevelDataData>();
        }

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
                QuerySnapshot existingSnapshot = await firestore.Collection(COLLECTION_TOWER_LEVEL_DATA)
                    .Limit(1)
                    .GetSnapshotAsync();
                
                if (existingSnapshot.Count > 0)
                {
                    Debug.Log($"[{GetServiceName()}] Collection {COLLECTION_TOWER_LEVEL_DATA} is not empty, skipping initialization");
                    return true;
                }

                Debug.Log($"[{GetServiceName()}] Collection {COLLECTION_TOWER_LEVEL_DATA} is empty, initializing...");
                return await InitializeTowerLevelDataCollectionAsync();
            }
            catch (Exception e)
            {
                Debug.LogError($"[{GetServiceName()}] Error checking/initializing collection: {e.Message}");
                return false;
            }
#endif
        }

#if FIREBASE_FIRESTORE_AVAILABLE
        private async Task<bool> InitializeTowerLevelDataCollectionAsync()
        {
            try
            {
                List<TowerLevelDataData> defaultTowerData = DefaultGameData.GetDefaultTowerLevelData();
                
                if (defaultTowerData == null || defaultTowerData.Count == 0)
                {
                    Debug.LogError($"[{GetServiceName()}] GetDefaultTowerLevelData returned empty list!");
                    return false;
                }
                
                Debug.Log($"[{GetServiceName()}] Preparing to initialize {defaultTowerData.Count} tower types");
                
                QuerySnapshot existingSnapshot = await firestore.Collection(COLLECTION_TOWER_LEVEL_DATA)
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
                    if (int.TryParse(doc.Id, out int docIdAsType))
                    {
                        existingTypes.Add(docIdAsType);
                    }
                }
                
                int createdCount = 0;
                int skippedCount = 0;
                
                foreach (var towerData in defaultTowerData)
                {
                    if (!DefaultGameData.IsValidTowerType(towerData.type))
                    {
                        skippedCount++;
                        continue;
                    }
                    
                    string docId = towerData.type.ToString("D2");
                    DocumentReference docRef = firestore.Collection(COLLECTION_TOWER_LEVEL_DATA).Document(docId);
                    
                    bool exists = existingTypes.Contains(towerData.type);
                    
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
                    
                    if (!exists)
                    {
                        data["createdAt"] = Timestamp.GetCurrentTimestamp();
                        await docRef.SetAsync(data);
                        createdCount++;
                    }
                    else
                    {
                        skippedCount++;
                    }
                }
                
                Debug.Log($"[{GetServiceName()}] ✅ Initialized {COLLECTION_TOWER_LEVEL_DATA}: {createdCount} created, {skippedCount} skipped");
                return true;
            }
            catch (Exception e)
            {
                string errorMsg = e.Message;
                if (errorMsg.Contains("permission") || errorMsg.Contains("Permission"))
                {
                    Debug.LogError($"[{GetServiceName()}] Permission denied when initializing {COLLECTION_TOWER_LEVEL_DATA}.");
                }
                else
                {
                    Debug.LogError($"[{GetServiceName()}] Error initializing {COLLECTION_TOWER_LEVEL_DATA}: {errorMsg}");
                }
                return false;
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
                Debug.LogError($"[{GetServiceName()}] Error parsing TowerLevelData: {e.Message}");
                return null;
            }
        }

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
#endif

        public override void Shutdown()
        {
            base.Shutdown();
            cachedTowerLevelData = null;
        }
    }
}

