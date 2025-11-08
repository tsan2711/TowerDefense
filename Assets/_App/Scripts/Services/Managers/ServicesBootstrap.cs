using UnityEngine;
using Services.Core;
using Services.Auth;
using Services.Firestore;
using Services.Config;

namespace Services.Managers
{
    /// <summary>
    /// Bootstrap script to initialize all services
    /// Attach this to a GameObject in your initial scene
    /// </summary>
    public class ServicesBootstrap : MonoBehaviour
    {
        [Header("Service Configuration")]
        [SerializeField] private bool autoInitializeOnAwake = true;
        
        [Header("Firebase Configuration")]
        [Tooltip("Firebase config data. If null, will try to load from Resources/FirebaseConfig")]
        [SerializeField] private FirebaseConfigData firebaseConfig;

        private void Awake()
        {
            if (autoInitializeOnAwake)
            {
                InitializeServices();
            }
        }

        /// <summary>
        /// Initialize all services
        /// </summary>
        public void InitializeServices()
        {
            // Prevent duplicate initialization
            if (ServiceLocator.Instance.IsServiceRegistered<IAuthService>())
            {
                Debug.Log("[ServicesBootstrap] Services already initialized, skipping...");
                return;
            }

            // Initialize Firebase Config Manager first
            InitializeFirebaseConfig();

            // Ensure ServiceLocator exists (it will be created automatically)
            _ = ServiceLocator.Instance;

            // Initialize Firebase Initialization Service first (must be before other Firebase services)
            InitializeFirebaseInitService();

            // Initialize Auth Service
            InitializeAuthService();

            // Initialize Microservices - following microservice architecture pattern
            InitializeAgentConfigurationService();
            InitializeTowerDataService();
            InitializeLevelManagementService();
            InitializeUserDataService();
            InitializeInventoryService();
            InitializeInventoryConfigService();

            // Legacy Firestore Service (deprecated - use individual microservices instead)
            // InitializeFirestoreService(); // Commented out to use microservices

            // Add other services here as you scale
            // InitializeAnalyticsService();
            // InitializeCloudStorageService();
            // etc.

            // Initialize all services
            ServiceLocator.Instance.InitializeAllServices();
        }

        /// <summary>
        /// Initialize Firebase Config Manager
        /// </summary>
        private void InitializeFirebaseConfig()
        {
            FirebaseConfigManager configManager = FirebaseConfigManager.Instance;
            if(firebaseConfig == null)
            {
                firebaseConfig = Resources.Load<FirebaseConfigData>("FirebaseConfig");
            }
            // Set config if assigned in inspector
            if (firebaseConfig != null)
            {
                configManager.SetConfig(firebaseConfig);
            }
            
            if (!configManager.IsInitialized)
            {
                Debug.LogWarning("[ServicesBootstrap] FirebaseConfig not initialized. " +
                    "Some Firebase services may not work correctly.");
            }
        }

        /// <summary>
        /// Initialize Firebase Initialization Service
        /// This must be initialized before other Firebase services
        /// </summary>
        private void InitializeFirebaseInitService()
        {
            // FirebaseInitializationService is a singleton, just ensure it exists
            Services.Core.FirebaseInitializationService firebaseInit = 
                Services.Core.FirebaseInitializationService.Instance;
            
            // Register it with ServiceLocator so it can be managed
            ServiceLocator.Instance.RegisterService<Services.Core.IService>(firebaseInit);
            
            // Start Firebase initialization immediately (không đợi InitializeAllServices)
            // Điều này giúp Firebase bắt đầu check dependencies sớm hơn
            if (!firebaseInit.IsInitialized && !firebaseInit.IsInitializing)
            {
                Debug.Log("[ServicesBootstrap] Starting Firebase initialization early...");
                firebaseInit.Initialize();
            }
        }

        private void InitializeAuthService()
        {
            // Check if auth service already exists
            if (ServiceLocator.Instance.IsServiceRegistered<IAuthService>())
            {
                return;
            }

            // Create Firebase Auth Service GameObject
            GameObject authServiceGO = new GameObject("FirebaseAuthService");
            FirebaseAuthService authService = authServiceGO.AddComponent<FirebaseAuthService>();
            
            // Register with ServiceLocator
            ServiceLocator.Instance.RegisterService<IAuthService>(authService);
        }

        private void InitializeFirestoreService()
        {
            // Check if firestore service already exists
            if (ServiceLocator.Instance.IsServiceRegistered<IFirestoreService>())
            {
                return;
            }

            // Create Firebase Firestore Service GameObject
            GameObject firestoreServiceGO = new GameObject("FirebaseFirestoreService");
            FirebaseFirestoreService firestoreService = firestoreServiceGO.AddComponent<FirebaseFirestoreService>();
            
            // Register with ServiceLocator
            ServiceLocator.Instance.RegisterService<IFirestoreService>(firestoreService);
        }

        private void InitializeAgentConfigurationService()
        {
            if (ServiceLocator.Instance.IsServiceRegistered<IAgentConfigurationService>())
            {
                return;
            }

            GameObject serviceGO = new GameObject("AgentConfigurationService");
            Firestore.AgentConfigurationService service = serviceGO.AddComponent<Firestore.AgentConfigurationService>();
            ServiceLocator.Instance.RegisterService<IAgentConfigurationService>(service);
        }

        private void InitializeTowerDataService()
        {
            if (ServiceLocator.Instance.IsServiceRegistered<ITowerDataService>())
            {
                return;
            }

            GameObject serviceGO = new GameObject("TowerDataService");
            Firestore.TowerDataService service = serviceGO.AddComponent<Firestore.TowerDataService>();
            ServiceLocator.Instance.RegisterService<ITowerDataService>(service);
        }

        private void InitializeLevelManagementService()
        {
            if (ServiceLocator.Instance.IsServiceRegistered<ILevelManagementService>())
            {
                return;
            }

            GameObject serviceGO = new GameObject("LevelManagementService");
            Firestore.LevelManagementService service = serviceGO.AddComponent<Firestore.LevelManagementService>();
            ServiceLocator.Instance.RegisterService<ILevelManagementService>(service);
        }

        private void InitializeUserDataService()
        {
            if (ServiceLocator.Instance.IsServiceRegistered<IUserDataService>())
            {
                return;
            }

            GameObject serviceGO = new GameObject("UserDataService");
            Firestore.UserDataService service = serviceGO.AddComponent<Firestore.UserDataService>();
            ServiceLocator.Instance.RegisterService<IUserDataService>(service);
        }

        private void InitializeInventoryService()
        {
            if (ServiceLocator.Instance.IsServiceRegistered<IInventoryService>())
            {
                return;
            }

            GameObject serviceGO = new GameObject("InventoryService");
            Firestore.InventoryService service = serviceGO.AddComponent<Firestore.InventoryService>();
            ServiceLocator.Instance.RegisterService<IInventoryService>(service);
        }

        private void InitializeInventoryConfigService()
        {
            if (ServiceLocator.Instance.IsServiceRegistered<IInventoryConfigService>())
            {
                return;
            }

            GameObject serviceGO = new GameObject("InventoryConfigService");
            Firestore.InventoryConfigService service = serviceGO.AddComponent<Firestore.InventoryConfigService>();
            ServiceLocator.Instance.RegisterService<IInventoryConfigService>(service);
        }

        /// <summary>
        /// Example: Initialize other services
        /// </summary>
        /*
        private void InitializeAnalyticsService()
        {
            GameObject analyticsGO = new GameObject("FirebaseAnalyticsService");
            // var analyticsService = analyticsGO.AddComponent<FirebaseAnalyticsService>();
            // ServiceLocator.Instance.RegisterService<IAnalyticsService>(analyticsService);
        }
        */

        private void OnDestroy()
        {
            // Cleanup if needed
        }
    }
}

