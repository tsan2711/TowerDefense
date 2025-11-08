using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Services.Core;
using Services.Firestore;
using Services.Managers;
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;

namespace Services.Auth
{
    /// <summary>
    /// Firebase implementation of IAuthService
    /// Handles Google Sign-In and Email/Password authentication
    /// </summary>
    public class FirebaseAuthService : MonoBehaviour, IAuthService
    {
        private FirebaseAuth auth;
        private bool isInitialized = false;
        private UserInfo currentUserInfo;
        private IInventoryService inventoryService; // Cache reference để subscribe events

        // Events
        public event Action<bool> OnAuthStateChanged;
        public event Action<UserInfo> OnSignInSuccess;
        public event Action<string> OnSignInFailed;

        public bool IsInitialized => isInitialized;
        public bool IsAuthenticated => auth?.CurrentUser != null;
        public UserInfo CurrentUser => currentUserInfo;

        private void Awake()
        {
            // Ensure this persists across scenes
            DontDestroyOnLoad(gameObject);
        }

        public void Initialize()
        {
            if (isInitialized)
            {
                Debug.LogWarning("[FirebaseAuthService] Already initialized");
                return;
            }

            // Use centralized Firebase initialization service
            FirebaseInitializationService firebaseInit = FirebaseInitializationService.Instance;
            
            if (firebaseInit.IsInitialized)
            {
                InitializeAuthInstance();
            }
            else
            {
                // Wait for Firebase to be initialized
                StartCoroutine(WaitForFirebaseInitialization());
            }
        }

        private IEnumerator WaitForFirebaseInitialization()
        {
            FirebaseInitializationService firebaseInit = FirebaseInitializationService.Instance;
            
            // Check if already initialized (có thể đã initialized sớm)
            if (firebaseInit.IsInitialized)
            {
                Debug.Log("[FirebaseAuthService] Firebase already initialized, initializing Auth immediately...");
                InitializeAuthInstance();
                yield break;
            }
            
            // Initialize Firebase if not already initializing
            if (!firebaseInit.IsInitialized && !firebaseInit.IsInitializing)
            {
                Debug.Log("[FirebaseAuthService] Firebase not initialized yet, starting initialization...");
                firebaseInit.Initialize();
            }
            else if (firebaseInit.IsInitializing)
            {
                Debug.Log("[FirebaseAuthService] Firebase is already initializing, waiting...");
            }

            // Wait for initialization using event-based approach thay vì polling Task
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
            var startTime = System.DateTime.Now;
            Task<bool> initTask = firebaseInit.WaitForInitializationAsync(30);
            
            // Wait until initialization completes or timeout
            float checkInterval = 0.1f;
            while (!initCompleted && !initTask.IsCompleted)
            {
                yield return new WaitForSeconds(checkInterval);
                var elapsed = (System.DateTime.Now - startTime).TotalSeconds;
                
                // Log progress every 2 seconds
                if (Mathf.FloorToInt((float)elapsed) % 2 == 0 && Mathf.FloorToInt((float)elapsed * 10) % 20 == 0)
                {
                    Debug.Log($"[FirebaseAuthService] Waiting for Firebase initialization... ({elapsed:F1}s)");
                }
            }
            
            // Remove handler if not already removed
            firebaseInit.OnInitializationComplete -= handler;

            // Determine result
            bool success = false;
            if (initCompleted)
            {
                success = initSuccess;
                var elapsed = (System.DateTime.Now - startTime).TotalSeconds;
                Debug.Log($"[FirebaseAuthService] Firebase initialization completed via event after {elapsed:F2}s");
            }
            else if (initTask.IsCompleted)
            {
                // Safe to access Result because we've checked IsCompleted
                // Using GetAwaiter().GetResult() to avoid any potential await issues
                try
                {
                    success = initTask.GetAwaiter().GetResult();
                    var elapsed = (System.DateTime.Now - startTime).TotalSeconds;
                    Debug.Log($"[FirebaseAuthService] Firebase initialization completed via task after {elapsed:F2}s");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[FirebaseAuthService] Error getting task result: {ex.Message}");
                    success = false;
                }
            }

            if (success && firebaseInit.DependencyStatus == DependencyStatus.Available)
            {
                Debug.Log("[FirebaseAuthService] Firebase initialized successfully, initializing Auth...");
                InitializeAuthInstance();
            }
            else
            {
                Debug.LogError($"[FirebaseAuthService] Firebase initialization failed. Dependency status: {firebaseInit.DependencyStatus}");
                OnAuthStateChanged?.Invoke(false);
            }
        }

        private void InitializeAuthInstance()
        {
            try
            {
                auth = FirebaseAuth.DefaultInstance;
                auth.StateChanged += OnAuthStateChangedHandler;
                auth.IdTokenChanged += OnIdTokenChangedHandler;

                // Check if user is already signed in
                UpdateCurrentUser();

                isInitialized = true;
                Debug.Log("[FirebaseAuthService] Initialized successfully");

                OnAuthStateChanged?.Invoke(IsAuthenticated);
                
                // ✅ Load data nếu user đã đăng nhập (session restored)
                if (IsAuthenticated && currentUserInfo != null)
                {
                    Debug.Log("[FirebaseAuthService] User session restored, loading configuration data...");
                    LoadConfigurationDataAfterSignIn();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FirebaseAuthService] Failed to initialize FirebaseAuth: {ex.Message}");
                Debug.LogError($"[FirebaseAuthService] Exception: {ex}");
                OnAuthStateChanged?.Invoke(false);
            }
        }

        public async Task<Services.Core.AuthResult> SignInWithGoogleAsync()
        {
            if (!isInitialized)
            {
                return Services.Core.AuthResult.CreateFailure("Service not initialized");
            }

            try
            {
                // Google Sign-In implementation
                // Note: You'll need to integrate Google Sign-In plugin for Unity
                // This is a placeholder that shows the structure
                
                var credential = await GetGoogleCredentialAsync();
                if (credential == null)
                {
                    return Services.Core.AuthResult.CreateFailure("Failed to get Google credential");
                }

                // SignInWithCredentialAsync returns Task<FirebaseUser>, not Task<AuthResult>
                FirebaseUser firebaseUser = await auth.SignInWithCredentialAsync(credential).ContinueWithOnMainThread(task =>
                {
                    if (task.IsFaulted)
                    {
                        throw task.Exception?.GetBaseException() ?? new Exception("Sign in failed");
                    }
                    if (task.IsCanceled)
                    {
                        throw new System.OperationCanceledException("Sign in was canceled");
                    }
                    return task.Result;
                });

                if (firebaseUser != null)
                {
                    currentUserInfo = CreateUserInfo(firebaseUser);
                    OnSignInSuccess?.Invoke(currentUserInfo);
                    OnAuthStateChanged?.Invoke(true);
                    
                    // Load configuration data from Firestore after successful sign in
                    LoadConfigurationDataAfterSignIn();
                    
                    return Services.Core.AuthResult.CreateSuccess(currentUserInfo);
                }

                return Services.Core.AuthResult.CreateFailure("Sign in failed");
            }
            catch (Exception e)
            {
                string errorMsg = $"Google sign-in error: {e.Message}";
                Debug.LogError($"[FirebaseAuthService] {errorMsg}");
                OnSignInFailed?.Invoke(errorMsg);
                return Services.Core.AuthResult.CreateFailure(errorMsg);
            }
        }

        public async Task<Services.Core.AuthResult> SignInWithEmailAsync(string email, string password)
        {
            if (!isInitialized)
            {
                return Services.Core.AuthResult.CreateFailure("Service not initialized");
            }

            try
            {
                Firebase.Auth.AuthResult firebaseResult = await auth.SignInWithEmailAndPasswordAsync(email, password);
                
                if (firebaseResult.User != null)
                {
                    currentUserInfo = CreateUserInfo(firebaseResult.User);
                    OnSignInSuccess?.Invoke(currentUserInfo);
                    OnAuthStateChanged?.Invoke(true);
                    
                    // Load configuration data from Firestore after successful sign in
                    LoadConfigurationDataAfterSignIn();
                    
                    return Services.Core.AuthResult.CreateSuccess(currentUserInfo);
                }

                return Services.Core.AuthResult.CreateFailure("Sign in failed");
            }
            catch (FirebaseException e)
            {
                string errorMsg = GetFirebaseErrorMessage(e);
                Debug.LogError($"[FirebaseAuthService] Email sign-in error: {errorMsg}");
                OnSignInFailed?.Invoke(errorMsg);
                return Services.Core.AuthResult.CreateFailure(errorMsg);
            }
            catch (Exception e)
            {
                string errorMsg = $"Email sign-in error: {e.Message}";
                Debug.LogError($"[FirebaseAuthService] {errorMsg}");
                OnSignInFailed?.Invoke(errorMsg);
                return Services.Core.AuthResult.CreateFailure(errorMsg);
            }
        }

        public async Task<Services.Core.AuthResult> SignUpWithEmailAsync(string email, string password)
        {
            if (!isInitialized)
            {
                return Services.Core.AuthResult.CreateFailure("Service not initialized");
            }

            try
            {
                Firebase.Auth.AuthResult firebaseResult = await auth.CreateUserWithEmailAndPasswordAsync(email, password);
                
                if (firebaseResult.User != null)
                {
                    currentUserInfo = CreateUserInfo(firebaseResult.User);
                    OnSignInSuccess?.Invoke(currentUserInfo);
                    OnAuthStateChanged?.Invoke(true);
                    
                    // Load configuration data from Firestore after successful sign up
                    LoadConfigurationDataAfterSignIn();
                    
                    return Services.Core.AuthResult.CreateSuccess(currentUserInfo);
                }

                return Services.Core.AuthResult.CreateFailure("Sign up failed");
            }
            catch (FirebaseException e)
            {
                string errorMsg = GetFirebaseErrorMessage(e);
                Debug.LogError($"[FirebaseAuthService] Email sign-up error: {errorMsg}");
                OnSignInFailed?.Invoke(errorMsg);
                return Services.Core.AuthResult.CreateFailure(errorMsg);
            }
            catch (Exception e)
            {
                string errorMsg = $"Email sign-up error: {e.Message}";
                Debug.LogError($"[FirebaseAuthService] {errorMsg}");
                OnSignInFailed?.Invoke(errorMsg);
                return Services.Core.AuthResult.CreateFailure(errorMsg);
            }
        }

        public async Task SignOutAsync()
        {
            if (!isInitialized || !IsAuthenticated)
            {
                return;
            }

            try
            {
                auth.SignOut();
                currentUserInfo = null;
                OnAuthStateChanged?.Invoke(false);
                Debug.Log("[FirebaseAuthService] Signed out successfully");
            }
            catch (Exception e)
            {
                Debug.LogError($"[FirebaseAuthService] Sign out error: {e.Message}");
            }

            await Task.CompletedTask;
        }

        public async Task<string> GetAuthTokenAsync()
        {
            if (!isInitialized || !IsAuthenticated)
            {
                return null;
            }

            try
            {
                var tokenResult = await auth.CurrentUser.TokenAsync(false);
                return tokenResult;
            }
            catch (Exception e)
            {
                Debug.LogError($"[FirebaseAuthService] Get token error: {e.Message}");
                return null;
            }
        }

        public void Shutdown()
        {
            if (auth != null)
            {
                auth.StateChanged -= OnAuthStateChangedHandler;
                auth.IdTokenChanged -= OnIdTokenChangedHandler;
            }

            // Unsubscribe from inventory events
            if (inventoryService != null)
            {
                inventoryService.OnInventoryLoaded -= OnInventoryLoaded;
            }

            isInitialized = false;
            currentUserInfo = null;
            inventoryService = null;
        }

        private void OnAuthStateChangedHandler(object sender, System.EventArgs eventArgs)
        {
            bool wasAuthenticated = IsAuthenticated; // Check trước khi update
            UpdateCurrentUser();
            bool isNowAuthenticated = IsAuthenticated;
            
            // ✅ Load data khi user chuyển từ unauthenticated → authenticated
            // Tránh load lại nếu đã authenticated trước đó (tránh duplicate call)
            if (isNowAuthenticated && !wasAuthenticated && currentUserInfo != null)
            {
                Debug.Log("[FirebaseAuthService] Auth state changed to authenticated, loading configuration data...");
                LoadConfigurationDataAfterSignIn();
            }
            
            OnAuthStateChanged?.Invoke(IsAuthenticated);
        }

        private void OnIdTokenChangedHandler(object sender, System.EventArgs eventArgs)
        {
            UpdateCurrentUser();
        }

        private void UpdateCurrentUser()
        {
            if (auth?.CurrentUser != null)
            {
                currentUserInfo = CreateUserInfo(auth.CurrentUser);
            }
            else
            {
                currentUserInfo = null;
            }
        }

        private UserInfo CreateUserInfo(FirebaseUser user)
        {
            return new UserInfo(
                uid: user.UserId,
                email: user.Email,
                displayName: user.DisplayName,
                photoURL: user.PhotoUrl?.ToString(),
                providerId: user.ProviderId
            );
        }

        private async Task<Credential> GetGoogleCredentialAsync()
        {
            // Use GoogleSignInHelper to get credential
            return await GoogleSignInHelper.GetGoogleCredentialAsync();
        }

        private string GetFirebaseErrorMessage(FirebaseException e)
        {
            // Map Firebase error codes to user-friendly messages
            switch (e.ErrorCode)
            {
                case (int)AuthError.EmailAlreadyInUse:
                    return "Email đã được sử dụng";
                case (int)AuthError.WrongPassword:
                    return "Mật khẩu không đúng";
                case (int)AuthError.UserNotFound:
                    return "Người dùng không tồn tại";
                case (int)AuthError.InvalidEmail:
                    return "Email không hợp lệ";
                case (int)AuthError.WeakPassword:
                    return "Mật khẩu quá yếu";
                case (int)AuthError.NetworkRequestFailed:
                    return "Lỗi kết nối mạng";
                default:
                    return $"Lỗi xác thực: {e.Message}";
            }
        }

        /// <summary>
        /// Load configuration data from Firestore after successful authentication
        /// </summary>
        private bool isLoadingConfigData = false; // Flag để tránh load nhiều lần đồng thời

        private async void LoadConfigurationDataAfterSignIn()
        {
            // ✅ Tránh load nhiều lần đồng thời
            if (isLoadingConfigData)
            {
                Debug.Log("[FirebaseAuthService] Configuration data is already loading, skipping...");
                return;
            }
            
            try
            {
                isLoadingConfigData = true;
                
                // Get microservices from ServiceLocator - following microservice architecture pattern
                IUserDataService userDataService = ServiceLocator.Instance.GetService<IUserDataService>();
                IAgentConfigurationService agentConfigService = ServiceLocator.Instance.GetService<IAgentConfigurationService>();
                ITowerDataService towerDataService = ServiceLocator.Instance.GetService<ITowerDataService>();
                ILevelManagementService levelManagementService = ServiceLocator.Instance.GetService<ILevelManagementService>();
                inventoryService = ServiceLocator.Instance.GetService<IInventoryService>();
                IInventoryConfigService inventoryConfigService = ServiceLocator.Instance.GetService<IInventoryConfigService>();
                
                // Subscribe to inventory events để tự động sync khi inventory thay đổi
                // Unsubscribe trước để tránh duplicate, rồi subscribe lại
                if (inventoryService != null)
                {
                    inventoryService.OnInventoryLoaded -= OnInventoryLoaded; // Unsubscribe trước (safe nếu chưa subscribe)
                    inventoryService.OnInventoryLoaded += OnInventoryLoaded;
                }
                
                // Save user data first (creates/updates user document in Firestore)
                if (currentUserInfo != null && userDataService != null && userDataService.IsInitialized)
                {
                    Debug.Log("[FirebaseAuthService] Saving user data to Firestore...");
                    bool saveSuccess = await userDataService.SaveUserDataAsync(currentUserInfo);
                    if (saveSuccess)
                    {
                        Debug.Log("[FirebaseAuthService] User data saved successfully to Firestore");
                    }
                    else
                    {
                        Debug.LogWarning("[FirebaseAuthService] Failed to save user data to Firestore");
                    }

                    // ✅ Load level progress after saving user data
                    Debug.Log("[FirebaseAuthService] Loading level progress from Firestore...");
                    await LoadUserLevelProgressAsync(userDataService);
                }

                // ✅ QUAN TRỌNG: Chỉ initialize collections khi chúng hoàn toàn empty
                // Không tạo/update nếu collections đã có data (preserve existing data)
                Debug.Log("[FirebaseAuthService] Checking if collections need initialization...");
                bool allInitSuccess = true;
                
                if (agentConfigService != null && agentConfigService.IsInitialized)
                {
                    bool initSuccess = await agentConfigService.InitializeCollectionIfEmptyAsync();
                    if (!initSuccess) allInitSuccess = false;
                }
                
                if (towerDataService != null && towerDataService.IsInitialized)
                {
                    bool initSuccess = await towerDataService.InitializeCollectionIfEmptyAsync();
                    if (!initSuccess) allInitSuccess = false;
                }
                
                if (levelManagementService != null && levelManagementService.IsInitialized)
                {
                    bool initSuccess = await levelManagementService.InitializeCollectionsIfEmptyAsync();
                    if (!initSuccess) allInitSuccess = false;
                }
                
                if (inventoryConfigService != null && inventoryConfigService.IsInitialized)
                {
                    bool initSuccess = await inventoryConfigService.InitializeCollectionIfEmptyAsync();
                    if (!initSuccess) allInitSuccess = false;
                }
                
                if (allInitSuccess)
                {
                    Debug.Log("[FirebaseAuthService] Collections check/initialization completed");
                }
                else
                {
                    Debug.LogWarning("[FirebaseAuthService] Some collections failed to initialize");
                }

                // ✅ Load configuration data từ backend (luôn load, không phụ thuộc vào initialization)
                Debug.Log("[FirebaseAuthService] Loading configuration data from Firestore microservices...");
                await LoadAllConfigurationDataAsync(agentConfigService, towerDataService, levelManagementService, inventoryConfigService);
                Debug.Log("[FirebaseAuthService] Configuration data loaded successfully");
                
                // ✅ Load user inventory after configuration data is loaded
                Debug.Log($"[FirebaseAuthService] Checking inventory service: currentUserInfo={currentUserInfo != null}, UID={currentUserInfo?.UID}, inventoryService={inventoryService != null}, IsInitialized={inventoryService?.IsInitialized}");
                
                if (currentUserInfo != null && !string.IsNullOrEmpty(currentUserInfo.UID) && inventoryService != null && inventoryService.IsInitialized)
                {
                    Debug.Log($"[FirebaseAuthService] Loading user inventory from Firestore for user: {currentUserInfo.UID}");
                    await LoadUserInventoryAsync(inventoryService, currentUserInfo.UID);
                    
                    // Sync inventory vào ScriptableObjects sau khi load xong
                    Services.Data.TowerInventoryData inventory = inventoryService?.GetCachedInventory();
                    Debug.Log($"[FirebaseAuthService] Cached inventory check: inventory={inventory != null}");
                    
                    if (inventory != null)
                    {
                        // Đảm bảo ownedTowers không null
                        if (inventory.ownedTowers == null)
                        {
                            inventory.ownedTowers = new List<Services.Data.InventoryItemData>();
                            Debug.LogWarning("[FirebaseAuthService] inventory.ownedTowers was null, initialized empty list");
                        }

                        Debug.Log($"[FirebaseAuthService] Syncing inventory to ScriptableObjects... (ownedTowers: {inventory.ownedTowers?.Count ?? 0})");
                        try
                        {
                            // Sync vào TowerInventory (selected towers cho gameplay)
                            GameDataSyncService.SyncUserInventoryToScriptableObject(inventory);
                            // Sync vào UserInventoryScriptableObject (all owned towers)
                            GameDataSyncService.SyncUserInventoryDataToScriptableObject(inventory);
                            Debug.Log("[FirebaseAuthService] ✅ Successfully synced inventory to ScriptableObjects");
                        }
                        catch (Exception syncEx)
                        {
                            Debug.LogError($"[FirebaseAuthService] Error syncing inventory to ScriptableObjects: {syncEx.Message}");
                            Debug.LogError($"[FirebaseAuthService] Stack trace: {syncEx.StackTrace}");
                        }
                    }
                    else
                    {
                        Debug.LogWarning("[FirebaseAuthService] Cached inventory is null, skipping sync. This might happen if inventory load failed.");
                    }
                }
                else
                {
                    Debug.LogWarning($"[FirebaseAuthService] Cannot load inventory - conditions not met: currentUserInfo={currentUserInfo != null}, UID empty={string.IsNullOrEmpty(currentUserInfo?.UID)}, inventoryService={inventoryService != null}, IsInitialized={inventoryService?.IsInitialized}");
                }

                // ✅ Initialize default stars again after level list is loaded (in case it wasn't available earlier)
                if (currentUserInfo != null && currentUserInfo.LevelProgress != null && userDataService != null && userDataService.IsInitialized)
                {
                    Debug.Log("[FirebaseAuthService] Level list loaded, checking and initializing default stars for all levels...");
                    await InitializeDefaultStarsForAllLevelsAsync(userDataService, currentUserInfo.LevelProgress);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[FirebaseAuthService] Error in post-sign-in operations: {e.Message}");
            }
            finally
            {
                isLoadingConfigData = false;
            }
        }

        /// <summary>
        /// Load all configuration data from microservices
        /// Microservice Pattern: Orchestrates data loading from multiple domain services
        /// </summary>
        private async Task LoadAllConfigurationDataAsync(
            IAgentConfigurationService agentConfigService,
            ITowerDataService towerDataService,
            ILevelManagementService levelManagementService,
            IInventoryConfigService inventoryConfigService)
        {
            try
            {
                // Load all data in parallel from microservices
                List<Task> loadTasks = new List<Task>();
                
                if (agentConfigService != null && agentConfigService.IsInitialized)
                {
                    loadTasks.Add(agentConfigService.LoadAgentConfigurationsAsync());
                }
                
                if (towerDataService != null && towerDataService.IsInitialized)
                {
                    loadTasks.Add(towerDataService.LoadTowerLevelDataAsync());
                }
                
                if (levelManagementService != null && levelManagementService.IsInitialized)
                {
                    loadTasks.Add(levelManagementService.LoadLevelListAsync());
                    loadTasks.Add(levelManagementService.LoadLevelLibraryConfigsAsync());
                }
                
                if (inventoryConfigService != null && inventoryConfigService.IsInitialized)
                {
                    loadTasks.Add(inventoryConfigService.LoadInventoryConfigAsync());
                }

                await Task.WhenAll(loadTasks);

                // Sync data vào ScriptableObjects sau khi load xong
                Debug.Log("[FirebaseAuthService] Syncing data to ScriptableObjects...");
                if (towerDataService != null)
                {
                    GameDataSyncService.SyncTowerLevelDataToScriptableObjects(towerDataService.GetCachedTowerLevelData());
                }
                
                if (agentConfigService != null)
                {
                    GameDataSyncService.SyncAgentConfigurationsToScriptableObjects(agentConfigService.GetCachedAgentConfigurations());
                }
                
                if (levelManagementService != null)
                {
                    GameDataSyncService.SyncLevelListToScriptableObject(levelManagementService.GetCachedLevelList());
                    GameDataSyncService.SyncLevelLibraryConfigsToContainer(levelManagementService.GetCachedLevelLibraryConfigs());
                }
                
                if (inventoryConfigService != null)
                {
                    GameDataSyncService.SyncInventoryConfigToScriptableObject(inventoryConfigService.GetCachedConfigs());
                }
                
                Debug.Log("[FirebaseAuthService] ScriptableObject sync completed");
            }
            catch (Exception e)
            {
                Debug.LogError($"[FirebaseAuthService] Error loading configuration data: {e.Message}");
            }
        }

        /// <summary>
        /// Load user level progress and update CurrentUser.LevelProgress
        /// </summary>
        private async Task LoadUserLevelProgressAsync(IUserDataService userDataService)
        {
            try
            {
                if (currentUserInfo == null || string.IsNullOrEmpty(currentUserInfo.UID))
                {
                    Debug.LogWarning("[FirebaseAuthService] Cannot load level progress: currentUserInfo is null or UID is empty");
                    return;
                }

                // Load level progress as object
                Services.Data.UserLevelProgress levelProgress = await userDataService.LoadLevelProgressObjectAsync(currentUserInfo.UID);
                
                if (levelProgress != null)
                {
                    // Update currentUserInfo.LevelProgress
                    currentUserInfo.LevelProgress = levelProgress;
                    Debug.Log($"[FirebaseAuthService] Level progress loaded: {levelProgress.LevelStars.Count} levels, maxLevel = {levelProgress.MaxLevel}");
                }
                else
                {
                    Debug.Log("[FirebaseAuthService] No level progress found in Firestore (new user or no data)");
                    // Initialize empty level progress if null
                    if (currentUserInfo.LevelProgress == null)
                    {
                        currentUserInfo.LevelProgress = new Services.Data.UserLevelProgress();
                    }
                    levelProgress = currentUserInfo.LevelProgress;
                }

                // ✅ Initialize default stars = 0 for all levels in level list (if user doesn't have them yet)
                await InitializeDefaultStarsForAllLevelsAsync(userDataService, levelProgress);
                
                // Also notify GameManager to load and sync with local data
                if (TowerDefense.Game.GameManager.instanceExists)
                {
                    // Trigger GameManager to refresh level progress from DB
                    // This will merge DB data with local data
                    TowerDefense.Game.GameManager.instance.RefreshLevelProgressFromDB();
                    Debug.Log("[FirebaseAuthService] Triggered GameManager to refresh level progress from DB");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[FirebaseAuthService] Error loading level progress: {e.Message}");
                // Initialize empty level progress on error
                if (currentUserInfo != null && currentUserInfo.LevelProgress == null)
                {
                    currentUserInfo.LevelProgress = new Services.Data.UserLevelProgress();
                }
            }
        }

        /// <summary>
        /// Load user inventory from Firestore
        /// If inventory doesn't exist, initialize it with default towers
        /// </summary>
        private async Task LoadUserInventoryAsync(IInventoryService inventoryService, string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    Debug.LogWarning("[FirebaseAuthService] Cannot load inventory: userId is empty");
                    return;
                }

                // Load user inventory
                Services.Data.TowerInventoryData inventory = await inventoryService?.LoadUserInventoryAsync(userId);
                
                if (inventory != null)
                {
                    // Đảm bảo ownedTowers không null
                    if (inventory.ownedTowers == null)
                    {
                        inventory.ownedTowers = new List<Services.Data.InventoryItemData>();
                        Debug.LogWarning("[FirebaseAuthService] inventory.ownedTowers was null after load, initialized empty list");
                    }

                    int ownedCount = inventory.ownedTowers?.Count ?? 0;
                    int selectedCount = inventory.GetSelectedCount();
                    Debug.Log($"[FirebaseAuthService] Inventory loaded: {ownedCount} towers owned, {selectedCount} selected");
                }
                else
                {
                    // Initialize inventory for new user if it doesn't exist
                    Debug.Log("[FirebaseAuthService] No inventory found, initializing new user inventory...");
                    bool initSuccess = inventoryService != null ? await inventoryService.InitializeUserInventoryAsync(userId) : false;
                    if (initSuccess)
                    {
                        Debug.Log("[FirebaseAuthService] User inventory initialized successfully");
                        // Reload after initialization
                        inventory = await inventoryService?.LoadUserInventoryAsync(userId);
                        if (inventory != null)
                        {
                            // Đảm bảo ownedTowers không null
                            if (inventory.ownedTowers == null)
                            {
                                inventory.ownedTowers = new List<Services.Data.InventoryItemData>();
                                Debug.LogWarning("[FirebaseAuthService] inventory.ownedTowers was null after init, initialized empty list");
                            }

                            int ownedCount = inventory.ownedTowers?.Count ?? 0;
                            Debug.Log($"[FirebaseAuthService] Inventory loaded after initialization: {ownedCount} towers owned");
                        }
                    }
                    else
                    {
                        Debug.LogWarning("[FirebaseAuthService] Failed to initialize user inventory");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[FirebaseAuthService] Error loading user inventory: {e.Message}");
            }
        }

        /// <summary>
        /// Initialize default stars = 0 for all levels in level list if user doesn't have them yet
        /// </summary>
        private async Task InitializeDefaultStarsForAllLevelsAsync(IUserDataService userDataService, Services.Data.UserLevelProgress levelProgress)
        {
            try
            {
                if (levelProgress == null)
                {
                    Debug.LogWarning("[FirebaseAuthService] Cannot initialize default stars: levelProgress is null");
                    return;
                }

                // Get level list from LevelManagementService
                ILevelManagementService levelManagementService = ServiceLocator.Instance.GetService<ILevelManagementService>();
                if (levelManagementService == null)
                {
                    Debug.LogWarning("[FirebaseAuthService] LevelManagementService not available, cannot initialize default stars");
                    return;
                }

                // Load level list if not cached yet (service might be initialized but cache might be empty)
                Services.Data.LevelListData levelList = levelManagementService.GetCachedLevelList();
                if (levelList == null || levelList.levels == null || levelList.levels.Count == 0)
                {
                    Debug.Log("[FirebaseAuthService] Level list not cached yet, loading from Firestore...");
                    if (levelManagementService.IsInitialized)
                    {
                        levelList = await levelManagementService.LoadLevelListAsync();
                    }
                    else
                    {
                        Debug.LogWarning("[FirebaseAuthService] LevelManagementService not initialized, cannot load level list");
                        return;
                    }
                }

                if (levelList == null || levelList.levels == null || levelList.levels.Count == 0)
                {
                    Debug.LogWarning("[FirebaseAuthService] Level list is empty, cannot initialize default stars");
                    return;
                }

                bool hasNewLevels = false;
                int addedCount = 0;

                // Check each level in level list
                foreach (var levelItem in levelList.levels)
                {
                    if (string.IsNullOrEmpty(levelItem.id))
                    {
                        continue;
                    }

                    // If user doesn't have this level in LevelStars, add it with default 0 stars
                    if (!levelProgress.LevelStars.ContainsKey(levelItem.id))
                    {
                        levelProgress.LevelStars[levelItem.id] = 0;
                        hasNewLevels = true;
                        addedCount++;
                        Debug.Log($"[FirebaseAuthService] Added default stars = 0 for level: {levelItem.id}");
                    }
                }

                // If we added new levels, save to Firestore
                if (hasNewLevels)
                {
                    Debug.Log($"[FirebaseAuthService] Initialized default stars = 0 for {addedCount} new levels. Saving to Firestore...");
                    bool saveSuccess = await userDataService.SaveLevelProgressAsync(currentUserInfo.UID, levelProgress);
                    if (saveSuccess)
                    {
                        Debug.Log($"[FirebaseAuthService] Successfully saved default stars for {addedCount} levels to Firestore");
                    }
                    else
                    {
                        Debug.LogWarning($"[FirebaseAuthService] Failed to save default stars to Firestore");
                    }
                }
                else
                {
                    Debug.Log("[FirebaseAuthService] All levels already have progress data, no initialization needed");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[FirebaseAuthService] Error initializing default stars: {e.Message}");
            }
        }

        /// <summary>
        /// Handle inventory loaded event - tự động sync vào ScriptableObjects
        /// </summary>
        private void OnInventoryLoaded(Services.Data.TowerInventoryData inventory)
        {
            if (inventory != null)
            {
                // Đảm bảo ownedTowers không null
                if (inventory.ownedTowers == null)
                {
                    inventory.ownedTowers = new List<Services.Data.InventoryItemData>();
                    Debug.LogWarning("[FirebaseAuthService] inventory.ownedTowers was null in event, initialized empty list");
                }

                Debug.Log("[FirebaseAuthService] Inventory loaded event received, syncing to ScriptableObjects...");
                // Sync vào TowerInventory (selected towers cho gameplay)
                GameDataSyncService.SyncUserInventoryToScriptableObject(inventory);
                // Sync vào UserInventoryScriptableObject (all owned towers)
                GameDataSyncService.SyncUserInventoryDataToScriptableObject(inventory);
            }
            else
            {
                Debug.LogWarning("[FirebaseAuthService] OnInventoryLoaded received null inventory");
            }
        }

        private void OnDestroy()
        {
            Shutdown();
        }
    }
}

