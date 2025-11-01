using System;
using System.Threading.Tasks;
using UnityEngine;
using Services.Core;
using Firebase;
using Firebase.Extensions;

namespace Services.Core
{
    /// <summary>
    /// Centralized Firebase initialization service
    /// Ensures Firebase dependencies are checked only once
    /// Other Firebase services should wait for this service to initialize before using Firebase
    /// </summary>
    public class FirebaseInitializationService : MonoBehaviour, IService
    {
        private static FirebaseInitializationService instance;
        private bool isInitialized = false;
        private bool isInitializing = false;
        private DependencyStatus dependencyStatus = DependencyStatus.UnavailableOther;
        private FirebaseApp firebaseApp;

        // Events
        public event Action<bool> OnInitializationComplete;
        public event Action<DependencyStatus> OnDependencyStatusChanged;

        public static FirebaseInitializationService Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject go = new GameObject("FirebaseInitializationService");
                    instance = go.AddComponent<FirebaseInitializationService>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        public bool IsInitialized => isInitialized;
        public bool IsInitializing => isInitializing;
        public DependencyStatus DependencyStatus => dependencyStatus;
        public FirebaseApp FirebaseApp => firebaseApp;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void Initialize()
        {
            if (isInitialized)
            {
                Debug.LogWarning("[FirebaseInitializationService] Already initialized");
                OnInitializationComplete?.Invoke(true);
                return;
            }

            if (isInitializing)
            {
                Debug.LogWarning("[FirebaseInitializationService] Already initializing, please wait...");
                return;
            }

            isInitializing = true;
            Debug.Log("[FirebaseInitializationService] Starting Firebase dependency check...");

            try
            {
                var startTime = System.DateTime.Now;
                FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
                {
                    var elapsed = (System.DateTime.Now - startTime).TotalSeconds;
                    Debug.Log($"[FirebaseInitializationService] Dependency check completed in {elapsed:F2} seconds");
                    
                    isInitializing = false;

                    if (task.IsFaulted)
                    {
                        Exception exception = task.Exception?.GetBaseException() ?? new Exception("Unknown error");
                        Debug.LogError($"[FirebaseInitializationService] Failed to check dependencies: {exception.Message}");
                        Debug.LogError($"[FirebaseInitializationService] Exception type: {exception.GetType().Name}");
                        Debug.LogError($"[FirebaseInitializationService] Stack trace: {exception.StackTrace}");
                        
                        // Check for DllNotFoundException specifically
                        if (exception is System.DllNotFoundException || 
                            exception is System.TypeInitializationException ||
                            (exception.InnerException != null && 
                             (exception.InnerException is System.DllNotFoundException || 
                              exception.InnerException is System.TypeInitializationException)))
                        {
                            Debug.LogError("[FirebaseInitializationService] Firebase native libraries not found. " +
                                         "Please ensure Firebase SDK is properly installed and native dependencies are resolved.");
                            Debug.LogError("[FirebaseInitializationService] For macOS Editor: Make sure you're using compatible Firebase SDK version.");
                            Debug.LogError("[FirebaseInitializationService] Try: Assets → External Dependency Manager → Android Resolver → Force Resolve");
                        }
                        
                        dependencyStatus = DependencyStatus.UnavailableOther;
                        OnDependencyStatusChanged?.Invoke(dependencyStatus);
                        OnInitializationComplete?.Invoke(false);
                        return;
                    }

                    if (task.IsCanceled)
                    {
                        Debug.LogWarning("[FirebaseInitializationService] Dependency check was canceled");
                        dependencyStatus = DependencyStatus.UnavailableOther;
                        OnDependencyStatusChanged?.Invoke(dependencyStatus);
                        OnInitializationComplete?.Invoke(false);
                        return;
                    }

                    dependencyStatus = task.Result;
                    Debug.Log($"[FirebaseInitializationService] Dependency status: {dependencyStatus}");
                    OnDependencyStatusChanged?.Invoke(dependencyStatus);

                    if (dependencyStatus == DependencyStatus.Available)
                    {
                        try
                        {
                            Debug.Log("[FirebaseInitializationService] Getting FirebaseApp.DefaultInstance...");
                            firebaseApp = FirebaseApp.DefaultInstance;
                            isInitialized = true;
                            
                            var totalTime = (System.DateTime.Now - startTime).TotalSeconds;
                            Debug.Log($"[FirebaseInitializationService] ✅ Firebase initialized successfully in {totalTime:F2} seconds");
                            OnInitializationComplete?.Invoke(true);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"[FirebaseInitializationService] Failed to get FirebaseApp instance: {ex.Message}");
                            Debug.LogError($"[FirebaseInitializationService] Exception: {ex}");
                            OnInitializationComplete?.Invoke(false);
                        }
                    }
                    else
                    {
                        Debug.LogError($"[FirebaseInitializationService] ❌ Could not resolve all Firebase dependencies: {dependencyStatus}");
                        Debug.LogError($"[FirebaseInitializationService] This may take longer on first run or if dependencies need to be downloaded.");
                        OnInitializationComplete?.Invoke(false);
                    }
                });
            }
            catch (System.DllNotFoundException ex)
            {
                isInitializing = false;
                Debug.LogError($"[FirebaseInitializationService] Firebase native library not found: {ex.Message}");
                Debug.LogError("[FirebaseInitializationService] This usually means Firebase SDK is not properly installed.");
                dependencyStatus = DependencyStatus.UnavailableOther;
                OnDependencyStatusChanged?.Invoke(dependencyStatus);
                OnInitializationComplete?.Invoke(false);
            }
            catch (System.TypeInitializationException ex)
            {
                isInitializing = false;
                Debug.LogError($"[FirebaseInitializationService] Firebase type initialization failed: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Debug.LogError($"[FirebaseInitializationService] Inner exception: {ex.InnerException.Message}");
                }
                Debug.LogError("[FirebaseInitializationService] This usually indicates missing native libraries or incompatible SDK version.");
                
                #if UNITY_EDITOR
                if (Application.platform == RuntimePlatform.OSXEditor)
                {
                    Debug.LogError("[FirebaseInitializationService] ⚠️ Đang chạy trên macOS Editor!");
                    Debug.LogError("[FirebaseInitializationService] Giải pháp:");
                    Debug.LogError("[FirebaseInitializationService] 1. Vào menu: Tools → Firebase → Fix Native Library Settings (macOS)");
                    Debug.LogError("[FirebaseInitializationService] 2. Sau khi chạy fixer, ĐÓNG VÀ MỞ LẠI Unity Editor");
                    Debug.LogError("[FirebaseInitializationService] 3. Thử chạy lại game");
                }
                #endif

                dependencyStatus = DependencyStatus.UnavailableOther;
                OnDependencyStatusChanged?.Invoke(dependencyStatus);
                OnInitializationComplete?.Invoke(false);
            }
            catch (Exception ex)
            {
                isInitializing = false;
                Debug.LogError($"[FirebaseInitializationService] Unexpected error during initialization: {ex.Message}");
                Debug.LogError($"[FirebaseInitializationService] Exception type: {ex.GetType().Name}");
                Debug.LogError($"[FirebaseInitializationService] Stack trace: {ex.StackTrace}");
                dependencyStatus = DependencyStatus.UnavailableOther;
                OnDependencyStatusChanged?.Invoke(dependencyStatus);
                OnInitializationComplete?.Invoke(false);
            }
        }

        /// <summary>
        /// Wait for Firebase to be initialized (async)
        /// Returns immediately when initialized or after timeout
        /// </summary>
        public async Task<bool> WaitForInitializationAsync(int timeoutSeconds = 30)
        {
            // If already initialized, return immediately
            if (isInitialized)
            {
                return true;
            }

            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
            Action<bool> handler = null;
            
            handler = (success) =>
            {
                if (!tcs.Task.IsCompleted)
                {
                    OnInitializationComplete -= handler;
                    tcs.SetResult(success);
                }
            };
            
            OnInitializationComplete += handler;

            // Race between initialization completion and timeout
            // Sử dụng Task.WhenAny để trả về ngay khi một trong hai hoàn thành
            Task<bool> initTask = tcs.Task;
            Task timeoutTask = Task.Delay(timeoutSeconds * 1000);
            
            var completedTask = await Task.WhenAny(initTask, timeoutTask);
            
            // Nếu timeout đến trước, cancel và return false
            if (completedTask == timeoutTask)
            {
                OnInitializationComplete -= handler;
                if (!tcs.Task.IsCompleted)
                {
                    tcs.SetResult(false);
                }
                return false;
            }
            
            // Nếu initialization hoàn thành, return result
            return await initTask;
        }

        public void Shutdown()
        {
            isInitialized = false;
            isInitializing = false;
            firebaseApp = null;
            dependencyStatus = DependencyStatus.UnavailableOther;
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
            Shutdown();
        }
    }
}

