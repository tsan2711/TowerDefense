using UnityEngine;
using UnityEditor;
using Services.Config;

namespace Services.Editor
{
    /// <summary>
    /// Editor script để tạo và quản lý FirebaseConfigData asset
    /// </summary>
    public class FirebaseConfigDataEditor
    {
        private const string DEFAULT_PATH = "Assets/Resources";
        private const string ASSET_NAME = "FirebaseConfig.asset";

        [MenuItem("Assets/Create/Services/Firebase Config", priority = 1)]
        public static void CreateFirebaseConfig()
        {
            // Ensure Resources folder exists
            if (!AssetDatabase.IsValidFolder(DEFAULT_PATH))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }

            // Check if asset already exists
            string assetPath = $"{DEFAULT_PATH}/{ASSET_NAME}";
            FirebaseConfigData existingConfig = AssetDatabase.LoadAssetAtPath<FirebaseConfigData>(assetPath);
            
            if (existingConfig != null)
            {
                Selection.activeObject = existingConfig;
                EditorUtility.FocusProjectWindow();
                EditorGUIUtility.PingObject(existingConfig);
                Debug.Log($"[FirebaseConfigDataEditor] FirebaseConfig đã tồn tại tại: {assetPath}");
                return;
            }

            // Create new config
            FirebaseConfigData config = ScriptableObject.CreateInstance<FirebaseConfigData>();
            
            // Set default values based on existing google-services.json
            config.projectId = "towerdefense-2bbc5";
            config.projectNumber = "1025933766701";
            config.storageBucket = "towerdefense-2bbc5.firebasestorage.app";
            config.apiKey = "AIzaSyAvg8E6YFJN74v7FvFGw3owzh6lkWVoOuo";
            config.androidAppId = "1:1025933766701:android:de5c7b6ede12f7d9c408a4";
            config.androidPackageName = "com.tsang";
            config.environmentName = "production";
            config.enableDebugLogging = true;

            AssetDatabase.CreateAsset(config, assetPath);
            AssetDatabase.SaveAssets();

            Selection.activeObject = config;
            EditorUtility.FocusProjectWindow();
            EditorGUIUtility.PingObject(config);

            Debug.Log($"[FirebaseConfigDataEditor] ✅ Đã tạo FirebaseConfig tại: {assetPath}");
            Debug.Log("[FirebaseConfigDataEditor] Vui lòng kiểm tra và cập nhật các giá trị config nếu cần.");
        }

        [MenuItem("Tools/Firebase/Validate Firebase Config")]
        public static void ValidateConfig()
        {
            string assetPath = $"{DEFAULT_PATH}/{ASSET_NAME}";
            FirebaseConfigData config = AssetDatabase.LoadAssetAtPath<FirebaseConfigData>(assetPath);
            
            if (config == null)
            {
                Debug.LogWarning($"[FirebaseConfigDataEditor] Không tìm thấy FirebaseConfig tại: {assetPath}");
                Debug.LogWarning("[FirebaseConfigDataEditor] Tạo mới bằng cách: Assets → Create → Services → Firebase Config");
                return;
            }

            if (config.IsValid())
            {
                Debug.Log("[FirebaseConfigDataEditor] ✅ FirebaseConfig hợp lệ!");
                config.LogConfigInfo();
            }
            else
            {
                Debug.LogError("[FirebaseConfigDataEditor] ❌ FirebaseConfig không hợp lệ!");
            }

            Selection.activeObject = config;
            EditorUtility.FocusProjectWindow();
            EditorGUIUtility.PingObject(config);
        }
    }
}

