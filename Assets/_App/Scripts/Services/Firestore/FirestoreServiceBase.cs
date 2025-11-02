using System.Collections;
using UnityEngine;
using Firebase;
#if FIREBASE_FIRESTORE_AVAILABLE
using Firebase.Firestore;
#endif
using Services.Core;
using Services.Config;

namespace Services.Firestore
{
    /// <summary>
    /// Base class for all Firestore services
    /// Provides shared initialization logic for Firebase Firestore
    /// Microservice Pattern: Base class for domain-specific Firestore services
    /// </summary>
    public abstract class FirestoreServiceBase : MonoBehaviour, IService
    {
#if FIREBASE_FIRESTORE_AVAILABLE
        protected FirebaseFirestore firestore;
#else
        protected object firestore; // Placeholder when package not installed
#endif
        protected bool isInitialized = false;

        public bool IsInitialized => isInitialized;

        protected virtual void Awake()
        {
            // Ensure this persists across scenes
            DontDestroyOnLoad(gameObject);
        }

        public virtual void Initialize()
        {
#if !FIREBASE_FIRESTORE_AVAILABLE
            Debug.LogError($"[{GetServiceName()}] Firebase Firestore package chưa được cài đặt! " +
                "Vui lòng import package FirebaseFirestore.unitypackage. " +
                "Xem file FIREBASE_FIRESTORE_INSTALL.md để biết chi tiết.");
            return;
#else
            if (isInitialized)
            {
                Debug.LogWarning($"[{GetServiceName()}] Already initialized");
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
                Debug.Log($"[{GetServiceName()}] Firebase already initialized, initializing Firestore immediately...");
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
            System.Action<bool> handler = null;
            
            handler = (success) =>
            {
                initCompleted = true;
                initSuccess = success;
                firebaseInit.OnInitializationComplete -= handler;
            };
            
            firebaseInit.OnInitializationComplete += handler;
            
            // Also start async task for timeout protection
            System.Threading.Tasks.Task<bool> initTask = firebaseInit.WaitForInitializationAsync(30);
            
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
                Debug.LogError($"[{GetServiceName()}] Firebase initialization failed. Dependency status: {firebaseInit.DependencyStatus}");
            }
        }

        protected virtual void InitializeFirestoreInstance()
        {
            try
            {
                firestore = FirebaseFirestore.DefaultInstance;
                isInitialized = true;
                Debug.Log($"[{GetServiceName()}] Initialized successfully");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[{GetServiceName()}] Failed to initialize FirebaseFirestore: {ex.Message}");
                Debug.LogError($"[{GetServiceName()}] Exception: {ex}");
            }
        }
#endif

        public virtual void Shutdown()
        {
            isInitialized = false;
#if FIREBASE_FIRESTORE_AVAILABLE
            firestore = null;
#endif
        }

        protected virtual void OnDestroy()
        {
            Shutdown();
        }

        /// <summary>
        /// Get service name for logging
        /// </summary>
        protected abstract string GetServiceName();

        /// <summary>
        /// Get collection name from config or default
        /// </summary>
        protected string GetCollectionName(string configMethod, string defaultValue)
        {
            // This will be implemented by derived classes if needed
            // For now, use FirebaseConfigManager directly
            return defaultValue;
        }
    }
}

