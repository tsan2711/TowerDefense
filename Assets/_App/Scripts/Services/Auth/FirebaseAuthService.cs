using System;
using System.Threading.Tasks;
using UnityEngine;
using Services.Core;
using Services.Firestore;
using Services.Managers;
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using System.Collections;

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

            isInitialized = false;
            currentUserInfo = null;
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
                
                // Get FirestoreService from ServiceLocator
                IFirestoreService firestoreService = ServiceLocator.Instance.GetService<IFirestoreService>();
                
                if (firestoreService != null && firestoreService.IsInitialized)
                {
                    // Save user data first (creates/updates user document in Firestore)
                    if (currentUserInfo != null)
                    {
                        Debug.Log("[FirebaseAuthService] Saving user data to Firestore...");
                        bool saveSuccess = await firestoreService.SaveUserDataAsync(currentUserInfo);
                        if (saveSuccess)
                        {
                            Debug.Log("[FirebaseAuthService] User data saved successfully to Firestore");
                        }
                        else
                        {
                            Debug.LogWarning("[FirebaseAuthService] Failed to save user data to Firestore");
                        }
                    }

                    // ✅ QUAN TRỌNG: Chỉ initialize collections khi chúng hoàn toàn empty
                    // Không tạo/update nếu collections đã có data (preserve existing data)
                    Debug.Log("[FirebaseAuthService] Checking if collections need initialization...");
                    bool initSuccess = await firestoreService.InitializeCollectionsIfEmptyAsync();
                    if (initSuccess)
                    {
                        Debug.Log("[FirebaseAuthService] Collections check/initialization completed");
                    }
                    else
                    {
                        Debug.LogWarning("[FirebaseAuthService] Failed to check/initialize collections");
                    }

                    // ✅ Load configuration data từ backend (luôn load, không phụ thuộc vào initialization)
                    Debug.Log("[FirebaseAuthService] Loading configuration data from Firestore...");
                    await firestoreService.LoadAllConfigDataAsync();
                    Debug.Log("[FirebaseAuthService] Configuration data loaded successfully");
                }
                else
                {
                    Debug.LogWarning("[FirebaseAuthService] FirestoreService not available, skipping user data save and config data load");
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

        private void OnDestroy()
        {
            Shutdown();
        }
    }
}

