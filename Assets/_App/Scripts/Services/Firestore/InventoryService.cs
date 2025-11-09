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
    /// Microservice for managing User Tower Inventory
    /// Handles user's tower collection and selection (max 3 towers)
    /// </summary>
    public class InventoryService : FirestoreServiceBase, IInventoryService
    {
        private const string DEFAULT_COLLECTION_INVENTORY = "userInventory";
        private string COLLECTION_INVENTORY => DEFAULT_COLLECTION_INVENTORY;

        // Cached inventory data
        private TowerInventoryData cachedInventory;

        // Events
        public event Action<TowerInventoryData> OnInventoryLoaded;
        public event Action<List<string>> OnSelectedTowersChanged;

        protected override string GetServiceName() => "InventoryService";

        #region Load Inventory

        public async Task<TowerInventoryData> LoadUserInventoryAsync(string userId)
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

            if (string.IsNullOrEmpty(userId))
            {
                Debug.LogError($"[{GetServiceName()}] Invalid userId");
                return null;
            }

            try
            {
                DocumentReference docRef = firestore.Collection(COLLECTION_INVENTORY).Document(userId);
                DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();

                if (snapshot.Exists)
                {
                    cachedInventory = ParseInventoryData(snapshot.ToDictionary(), userId);
                    
                    // Đảm bảo ownedTowers không null
                    if (cachedInventory.ownedTowers == null)
                    {
                        cachedInventory.ownedTowers = new List<InventoryItemData>();
                        Debug.LogWarning($"[{GetServiceName()}] ownedTowers was null, initialized empty list");
                    }
                    
                    Debug.Log($"[{GetServiceName()}] Loaded inventory for user {userId}: {cachedInventory.ownedTowers.Count} towers");
                    
                    // Đảm bảo user luôn có Emp1 (nếu chưa có thì tự động thêm)
                    try
                    {
                        await EnsureEmp1TowerAsync(userId);
                    }
                    catch (Exception empEx)
                    {
                        Debug.LogError($"[{GetServiceName()}] Error ensuring Emp1: {empEx.Message}");
                        Debug.LogError($"[{GetServiceName()}] Stack trace: {empEx.StackTrace}");
                    }
                    
                    // Đảm bảo ownedTowers vẫn không null sau khi thêm Emp1
                    if (cachedInventory.ownedTowers == null)
                    {
                        cachedInventory.ownedTowers = new List<InventoryItemData>();
                    }
                    
                    // Kiểm tra lại xem Emp1 có trong inventory không
                    bool hasEmp1 = cachedInventory.HasTower("Emp1");
                    Debug.Log($"[{GetServiceName()}] Final inventory count: {cachedInventory.ownedTowers.Count} towers, HasEmp1: {hasEmp1}");
                    
                    // Nếu vẫn chưa có Emp1, thêm trực tiếp vào cache (fallback)
                    if (!hasEmp1 && cachedInventory.ownedTowers != null)
                    {
                        Debug.LogWarning($"[{GetServiceName()}] Emp1 still missing after EnsureEmp1TowerAsync, adding directly to cache...");
                        InventoryItemData emp1Tower = new InventoryItemData("Emp1", 0, false);
                        cachedInventory.AddTower(emp1Tower);
                        Debug.Log($"[{GetServiceName()}] Added Emp1 directly to cache. New count: {cachedInventory.ownedTowers.Count}");
                        
                        // Try to save async (don't await to avoid blocking)
                        _ = SaveInventoryAsync(cachedInventory).ContinueWith(task =>
                        {
                            if (task.Result)
                            {
                                Debug.Log($"[{GetServiceName()}] ✅ Saved Emp1 to Firestore after direct add");
                            }
                            else
                            {
                                Debug.LogError($"[{GetServiceName()}] ❌ Failed to save Emp1 to Firestore after direct add");
                            }
                        });
                    }
                    
                    OnInventoryLoaded?.Invoke(cachedInventory);
                    return cachedInventory;
                }
                else
                {
                    Debug.LogWarning($"[{GetServiceName()}] Inventory not found for user {userId}");
                    // Initialize new inventory
                    await InitializeUserInventoryAsync(userId);
                    return cachedInventory;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[{GetServiceName()}] Error loading inventory: {e.Message}");
                return null;
            }
#endif
        }

        public TowerInventoryData GetCachedInventory()
        {
            return cachedInventory;
        }

        #endregion

        #region Unlock/Remove Towers

        public async Task<bool> UnlockTowerAsync(string userId, string towerName)
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

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(towerName))
            {
                Debug.LogError($"[{GetServiceName()}] Invalid parameters");
                return false;
            }

            try
            {
                // Load current inventory
                if (cachedInventory == null || cachedInventory.userId != userId)
                {
                    await LoadUserInventoryAsync(userId);
                }

                if (cachedInventory.HasTower(towerName))
                {
                    Debug.LogWarning($"[{GetServiceName()}] Tower {towerName} already owned by user {userId}");
                    return false;
                }

                // Add tower to inventory
                InventoryItemData newTower = new InventoryItemData(towerName, 0); // towerType will be set from config
                cachedInventory.AddTower(newTower);

                // Save to Firestore
                bool success = await SaveInventoryAsync(cachedInventory);
                
                if (success)
                {
                    Debug.Log($"[{GetServiceName()}] Unlocked tower {towerName} for user {userId}");
                    OnInventoryLoaded?.Invoke(cachedInventory);
                }

                return success;
            }
            catch (Exception e)
            {
                Debug.LogError($"[{GetServiceName()}] Error unlocking tower: {e.Message}");
                return false;
            }
#endif
        }

        public async Task<bool> RemoveTowerAsync(string userId, string towerName)
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

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(towerName))
            {
                Debug.LogError($"[{GetServiceName()}] Invalid parameters");
                return false;
            }

            try
            {
                // Load current inventory
                if (cachedInventory == null || cachedInventory.userId != userId)
                {
                    await LoadUserInventoryAsync(userId);
                }

                if (!cachedInventory.HasTower(towerName))
                {
                    Debug.LogWarning($"[{GetServiceName()}] Tower {towerName} not owned by user {userId}");
                    return false;
                }

                // Remove tower
                cachedInventory.RemoveTower(towerName);

                // Save to Firestore
                bool success = await SaveInventoryAsync(cachedInventory);
                
                if (success)
                {
                    Debug.Log($"[{GetServiceName()}] Removed tower {towerName} from user {userId}");
                    OnInventoryLoaded?.Invoke(cachedInventory);
                    OnSelectedTowersChanged?.Invoke(cachedInventory.GetSelectedTowerNames());
                }

                return success;
            }
            catch (Exception e)
            {
                Debug.LogError($"[{GetServiceName()}] Error removing tower: {e.Message}");
                return false;
            }
#endif
        }

        #endregion

        #region Select Towers

        public async Task<bool> SelectTowersAsync(string userId, List<string> selectedTowerNames)
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

            if (string.IsNullOrEmpty(userId))
            {
                Debug.LogError($"[{GetServiceName()}] Invalid userId");
                return false;
            }

            if (selectedTowerNames == null || selectedTowerNames.Count > 3)
            {
                Debug.LogError($"[{GetServiceName()}] Invalid tower selection (max 3 towers)");
                return false;
            }

            try
            {
                // Load current inventory
                if (cachedInventory == null || cachedInventory.userId != userId)
                {
                    await LoadUserInventoryAsync(userId);
                }

                // Set selected towers
                bool validSelection = cachedInventory.SetSelectedTowers(selectedTowerNames);
                if (!validSelection)
                {
                    Debug.LogError($"[{GetServiceName()}] Invalid tower selection - one or more towers not owned");
                    return false;
                }

                // Save to Firestore
                bool success = await SaveInventoryAsync(cachedInventory);
                
                if (success)
                {
                    Debug.Log($"[{GetServiceName()}] Updated selected towers for user {userId}: {string.Join(", ", selectedTowerNames)}");
                    OnSelectedTowersChanged?.Invoke(selectedTowerNames);
                }

                return success;
            }
            catch (Exception e)
            {
                Debug.LogError($"[{GetServiceName()}] Error selecting towers: {e.Message}");
                return false;
            }
#endif
        }

        public List<string> GetSelectedTowers()
        {
            return cachedInventory?.GetSelectedTowerNames() ?? new List<string>();
        }

        #endregion

        #region Query Methods

        public bool HasTower(string towerName)
        {
            return cachedInventory?.HasTower(towerName) ?? false;
        }

        public List<string> GetAvailableTowers()
        {
            if (cachedInventory?.ownedTowers == null)
            {
                return new List<string>();
            }

            return cachedInventory.ownedTowers.Select(t => t.towerName).ToList();
        }

        #endregion

        #region Initialize

        public async Task<bool> InitializeUserInventoryAsync(string userId)
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

            if (string.IsNullOrEmpty(userId))
            {
                Debug.LogError($"[{GetServiceName()}] Invalid userId");
                return false;
            }

            try
            {
                DocumentReference docRef = firestore.Collection(COLLECTION_INVENTORY).Document(userId);
                DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();

                if (snapshot.Exists)
                {
                    Debug.Log($"[{GetServiceName()}] User {userId} already has inventory");
                    return false;
                }

                // Create new inventory with default towers
                TowerInventoryData newInventory = new TowerInventoryData(userId);
                
                // Add default tower: Emp1 (Emp tower type = 0, isSelected = false để hiển thị trong inventory grid)
                InventoryItemData defaultTower = new InventoryItemData("Emp1", 0, false); // type 0 = Emp
                newInventory.AddTower(defaultTower);

                cachedInventory = newInventory;

                // Save to Firestore
                bool success = await SaveInventoryAsync(newInventory);
                
                if (success)
                {
                    Debug.Log($"[{GetServiceName()}] Initialized inventory for user {userId}");
                    OnInventoryLoaded?.Invoke(cachedInventory);
                }

                return success;
            }
            catch (Exception e)
            {
                Debug.LogError($"[{GetServiceName()}] Error initializing inventory: {e.Message}");
                return false;
            }
#endif
        }

        #endregion

        #region Ensure Default Tower

        /// <summary>
        /// Đảm bảo user luôn có Emp1 tower (tự động thêm nếu chưa có)
        /// </summary>
        private async Task EnsureEmp1TowerAsync(string userId)
        {
            if (cachedInventory == null || cachedInventory.userId != userId)
            {
                Debug.LogWarning($"[{GetServiceName()}] Cannot ensure Emp1: cachedInventory is null or userId mismatch");
                return;
            }

            // Đảm bảo ownedTowers không null
            if (cachedInventory.ownedTowers == null)
            {
                cachedInventory.ownedTowers = new List<InventoryItemData>();
                Debug.LogWarning($"[{GetServiceName()}] ownedTowers was null in EnsureEmp1TowerAsync, initialized empty list");
            }

            const string EMP1_TOWER_NAME = "Emp1";
            const int EMP_TOWER_TYPE = 0; // MainTower.Emp = 0

            // Kiểm tra xem user đã có Emp1 chưa
            if (!cachedInventory.HasTower(EMP1_TOWER_NAME))
            {
                Debug.Log($"[{GetServiceName()}] User {userId} chưa có Emp1, đang tự động thêm...");
                
                // Thêm Emp1 vào inventory (isSelected = false để hiển thị trong inventory grid)
                InventoryItemData emp1Tower = new InventoryItemData(EMP1_TOWER_NAME, EMP_TOWER_TYPE, false);
                
                // Đảm bảo ownedTowers không null trước khi thêm
                if (cachedInventory.ownedTowers == null)
                {
                    cachedInventory.ownedTowers = new List<InventoryItemData>();
                }
                
                cachedInventory.AddTower(emp1Tower);
                Debug.Log($"[{GetServiceName()}] Added Emp1 to inventory. Total towers: {cachedInventory.ownedTowers.Count}");

                // Lưu vào Firestore
                bool success = await SaveInventoryAsync(cachedInventory);
                
                if (success)
                {
                    Debug.Log($"[{GetServiceName()}] ✅ Đã tự động thêm và lưu Emp1 cho user {userId}. Inventory count: {cachedInventory.ownedTowers.Count}");
                    // Không cần gọi OnInventoryLoaded ở đây vì đã được gọi trong LoadUserInventoryAsync
                }
                else
                {
                    Debug.LogError($"[{GetServiceName()}] ❌ Không thể lưu Emp1 cho user {userId}");
                }
            }
            else
            {
                // User đã có Emp1, kiểm tra xem có cần unselect không (optional)
                InventoryItemData existingEmp1 = cachedInventory.GetTower(EMP1_TOWER_NAME);
                if (existingEmp1 != null && existingEmp1.isSelected)
                {
                    Debug.Log($"[{GetServiceName()}] User {userId} đã có Emp1 và đang selected. Giữ nguyên trạng thái.");
                }
                else
                {
                    Debug.Log($"[{GetServiceName()}] User {userId} đã có Emp1, không cần thêm");
                }
            }
        }

        #endregion

        #region Helper Methods

#if FIREBASE_FIRESTORE_AVAILABLE
        private async Task<bool> SaveInventoryAsync(TowerInventoryData inventory)
        {
            try
            {
                DocumentReference docRef = firestore.Collection(COLLECTION_INVENTORY).Document(inventory.userId);

                Dictionary<string, object> data = new Dictionary<string, object>
                {
                    { "userId", inventory.userId },
                    { "maxSelectedTowers", inventory.maxSelectedTowers },
                    { "lastUpdated", Timestamp.GetCurrentTimestamp() }
                };

                // Convert owned towers to Firestore format
                List<Dictionary<string, object>> towersList = new List<Dictionary<string, object>>();
                if (inventory.ownedTowers != null)
                {
                    foreach (var tower in inventory.ownedTowers)
                    {
                        Dictionary<string, object> towerData = new Dictionary<string, object>
                        {
                            { "towerName", tower.towerName },
                            { "towerType", tower.towerType },
                            { "isSelected", tower.isSelected },
                            { "unlockedAt", tower.unlockedAt },
                            { "usageCount", tower.usageCount }
                        };
                        towersList.Add(towerData);
                    }
                }
                data["ownedTowers"] = towersList;

                await docRef.SetAsync(data);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[{GetServiceName()}] Error saving inventory: {e.Message}");
                return false;
            }
        }

        private TowerInventoryData ParseInventoryData(Dictionary<string, object> data, string userId)
        {
            try
            {
                TowerInventoryData inventory = new TowerInventoryData(userId);

                if (data.ContainsKey("maxSelectedTowers"))
                {
                    inventory.maxSelectedTowers = Convert.ToInt32(data["maxSelectedTowers"]);
                }

                if (data.ContainsKey("lastUpdated"))
                {
                    if (data["lastUpdated"] is Timestamp timestamp)
                    {
                        inventory.lastUpdated = new DateTimeOffset(timestamp.ToDateTime()).ToUnixTimeSeconds();
                    }
                    else
                    {
                        inventory.lastUpdated = Convert.ToInt64(data["lastUpdated"]);
                    }
                }

                // Parse owned towers
                if (data.ContainsKey("ownedTowers") && data["ownedTowers"] is List<object> towersList)
                {
                    foreach (var towerObj in towersList)
                    {
                        if (towerObj is Dictionary<string, object> towerData)
                        {
                            InventoryItemData item = new InventoryItemData
                            {
                                towerName = towerData.ContainsKey("towerName") ? towerData["towerName"].ToString() : "",
                                towerType = towerData.ContainsKey("towerType") ? Convert.ToInt32(towerData["towerType"]) : 0,
                                isSelected = towerData.ContainsKey("isSelected") && Convert.ToBoolean(towerData["isSelected"]),
                                unlockedAt = towerData.ContainsKey("unlockedAt") ? Convert.ToInt64(towerData["unlockedAt"]) : 0,
                                usageCount = towerData.ContainsKey("usageCount") ? Convert.ToInt32(towerData["usageCount"]) : 0
                            };
                            inventory.ownedTowers.Add(item);
                        }
                    }
                }

                return inventory;
            }
            catch (Exception e)
            {
                Debug.LogError($"[{GetServiceName()}] Error parsing inventory data: {e.Message}");
                return new TowerInventoryData(userId);
            }
        }
#endif

        #endregion
    }
}

