using UnityEngine;

namespace Services.Config
{
    /// <summary>
    /// ScriptableObject để lưu trữ cấu hình Firebase
    /// Cho phép quản lý config dễ dàng trong Unity Editor và switch giữa các môi trường
    /// </summary>
    [CreateAssetMenu(fileName = "FirebaseConfig", menuName = "Services/Firebase Config", order = 1)]
    public class FirebaseConfigData : ScriptableObject
    {
        [Header("Project Information")]
        [Tooltip("Firebase Project ID")]
        public string projectId = "towerdefense-2bbc5";
        
        [Tooltip("Firebase Project Number")]
        public string projectNumber = "1025933766701";
        
        [Tooltip("Storage Bucket")]
        public string storageBucket = "towerdefense-2bbc5.firebasestorage.app";

        [Header("API Configuration")]
        [Tooltip("Firebase API Key")]
        public string apiKey = "AIzaSyAvg8E6YFJN74v7FvFGw3owzh6lkWVoOuo";

        [Header("App Configuration")]
        [Tooltip("Mobile SDK App ID (Android)")]
        public string androidAppId = "1:1025933766701:android:de5c7b6ede12f7d9c408a4";
        
        [Tooltip("Package Name (Android)")]
        public string androidPackageName = "com.tsang";

        [Header("Firestore Collections")]
        [Tooltip("Tên collection cho AgentConfigurations")]
        public string agentConfigurationsCollection = "AgentConfigurations";
        
        [Tooltip("Tên collection cho TowerLevelData")]
        public string towerLevelDataCollection = "TowerLevelData";
        
        [Tooltip("Tên collection cho LevelList")]
        public string levelListCollection = "LevelList";
        
        [Tooltip("Tên collection cho LevelLibraryConfig")]
        public string levelLibraryConfigCollection = "LevelLibraryConfig";

        [Header("Environment Settings")]
        [Tooltip("Tên môi trường (dev, staging, production)")]
        public string environmentName = "production";
        
        [Tooltip("Enable debug logging")]
        public bool enableDebugLogging = true;

        /// <summary>
        /// Validate config data
        /// </summary>
        public bool IsValid()
        {
            if (string.IsNullOrEmpty(projectId))
            {
                Debug.LogError("[FirebaseConfigData] Project ID is required");
                return false;
            }
            
            if (string.IsNullOrEmpty(apiKey))
            {
                Debug.LogError("[FirebaseConfigData] API Key is required");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Get formatted project info for logging
        /// </summary>
        public string GetProjectInfo()
        {
            return $"Project: {projectId} ({environmentName})";
        }

        /// <summary>
        /// Log config info (without sensitive data)
        /// </summary>
        public void LogConfigInfo()
        {
            if (!enableDebugLogging) return;
            
            Debug.Log($"[FirebaseConfigData] {GetProjectInfo()}");
            Debug.Log($"[FirebaseConfigData] Storage Bucket: {storageBucket}");
            Debug.Log($"[FirebaseConfigData] API Key: {apiKey.Substring(0, Mathf.Min(20, apiKey.Length))}...");
        }
    }
}

