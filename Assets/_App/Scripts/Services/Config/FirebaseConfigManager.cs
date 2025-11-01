using UnityEngine;
using Services.Config;

namespace Services.Managers
{
    /// <summary>
    /// Manager để load và quản lý Firebase configuration từ ScriptableObject
    /// Singleton pattern để truy cập config từ bất kỳ đâu trong code
    /// </summary>
    public class FirebaseConfigManager : MonoBehaviour
    {
        private static FirebaseConfigManager instance;
        
        [Header("Configuration")]
        [SerializeField] private FirebaseConfigData configData;
        
        private bool isInitialized = false;

        public static FirebaseConfigManager Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject go = new GameObject("FirebaseConfigManager");
                    instance = go.AddComponent<FirebaseConfigManager>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        public FirebaseConfigData Config => configData;
        public bool IsInitialized => isInitialized && configData != null && configData.IsValid();

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            
            InitializeConfig();
        }

        /// <summary>
        /// Initialize config - try to load from Resources if not assigned
        /// </summary>
        private void InitializeConfig()
        {
            if (configData != null)
            {
                ValidateAndLogConfig();
                return;
            }

            // Try to load from Resources
            configData = Resources.Load<FirebaseConfigData>("FirebaseConfig");
            
            if (configData == null)
            {
                Debug.LogWarning("[FirebaseConfigManager] FirebaseConfigData not found. " +
                    "Please create one via 'Assets/Create/Services/Firebase Config' and assign it to ServicesBootstrap or place in Resources/FirebaseConfig");
                return;
            }

            ValidateAndLogConfig();
        }

        /// <summary>
        /// Validate and log config info
        /// </summary>
        private void ValidateAndLogConfig()
        {
            if (configData == null)
            {
                Debug.LogError("[FirebaseConfigManager] Config data is null!");
                return;
            }

            if (configData.IsValid())
            {
                isInitialized = true;
                configData.LogConfigInfo();
            }
            else
            {
                Debug.LogError("[FirebaseConfigManager] Config data is invalid!");
            }
        }

        /// <summary>
        /// Set config data programmatically
        /// </summary>
        public void SetConfig(FirebaseConfigData config)
        {
            configData = config;
            ValidateAndLogConfig();
        }

        /// <summary>
        /// Get API Key
        /// </summary>
        public string GetApiKey()
        {
            return IsInitialized ? configData.apiKey : null;
        }

        /// <summary>
        /// Get Project ID
        /// </summary>
        public string GetProjectId()
        {
            return IsInitialized ? configData.projectId : null;
        }

        /// <summary>
        /// Get Storage Bucket
        /// </summary>
        public string GetStorageBucket()
        {
            return IsInitialized ? configData.storageBucket : null;
        }

        /// <summary>
        /// Get collection name for AgentConfigurations
        /// </summary>
        public string GetAgentConfigurationsCollection()
        {
            return IsInitialized ? configData.agentConfigurationsCollection : "AgentConfigurations";
        }

        /// <summary>
        /// Get collection name for TowerLevelData
        /// </summary>
        public string GetTowerLevelDataCollection()
        {
            return IsInitialized ? configData.towerLevelDataCollection : "TowerLevelData";
        }

        /// <summary>
        /// Get collection name for LevelList
        /// </summary>
        public string GetLevelListCollection()
        {
            return IsInitialized ? configData.levelListCollection : "LevelList";
        }
    }
}

