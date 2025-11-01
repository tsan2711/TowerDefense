using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Services.Data;
using TowerDefense.Towers.Data;
using TowerDefense.Agents.Data;
using Core.Game;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Services.Firestore
{
    /// <summary>
    /// Service để sync data từ Firestore vào ScriptableObject
    /// Tự động tạo và update ScriptableObjects khi data được load từ Firestore
    /// </summary>
    public static class GameDataSyncService
    {
        /// <summary>
        /// Sync TowerLevelData từ Firestore vào ScriptableObjects
        /// Tạo hoặc update ScriptableObjects trong Resources/TowerData/
        /// </summary>
        public static void SyncTowerLevelDataToScriptableObjects(List<TowerLevelDataData> firestoreData)
        {
            if (firestoreData == null || firestoreData.Count == 0)
            {
                Debug.LogWarning("[GameDataSyncService] No tower data to sync");
                return;
            }

#if UNITY_EDITOR
            // Tạo folder nếu chưa có
            string folderPath = "Assets/Resources/TowerData";
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                {
                    AssetDatabase.CreateFolder("Assets", "Resources");
                }
                AssetDatabase.CreateFolder("Assets/Resources", "TowerData");
            }

            int syncedCount = 0;
            int createdCount = 0;
            int updatedCount = 0;

            foreach (var data in firestoreData)
            {
                try
                {
                    TowerType towerType = (TowerType)data.type;
                    string assetPath = $"{folderPath}/TowerLevelData_{towerType}.asset";

                    // Load existing hoặc tạo mới
                    TowerLevelData scriptableObject = AssetDatabase.LoadAssetAtPath<TowerLevelData>(assetPath);

                    if (scriptableObject == null)
                    {
                        scriptableObject = ScriptableObject.CreateInstance<TowerLevelData>();
                        AssetDatabase.CreateAsset(scriptableObject, assetPath);
                        createdCount++;
                        Debug.Log($"[GameDataSyncService] Created ScriptableObject: {assetPath}");
                    }
                    else
                    {
                        updatedCount++;
                        Debug.Log($"[GameDataSyncService] Updating ScriptableObject: {assetPath}");
                    }

                    // Update data từ Firestore
                    scriptableObject.towerType = towerType;
                    scriptableObject.description = data.description ?? "";
                    scriptableObject.upgradeDescription = data.upgradeDescription ?? "";
                    scriptableObject.cost = data.cost;
                    scriptableObject.sell = data.sell;
                    scriptableObject.maxHealth = data.maxHealth;
                    scriptableObject.startingHealth = data.startingHealth;
                    // Note: icon không được sync vì cần reference từ Unity (phải set manual trong Editor)

                    EditorUtility.SetDirty(scriptableObject);
                    syncedCount++;
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[GameDataSyncService] Error syncing tower type {data.type}: {ex.Message}");
                    Debug.LogError($"[GameDataSyncService] Exception: {ex}");
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[GameDataSyncService] ✅ Synced {syncedCount} tower ScriptableObjects ({createdCount} created, {updatedCount} updated)");
#else
            Debug.LogWarning("[GameDataSyncService] ScriptableObject sync chỉ hoạt động trong Editor mode. " +
                           "Runtime: Sử dụng data từ cache thay vì ScriptableObject.");
#endif
        }

        /// <summary>
        /// Sync AgentConfiguration từ Firestore vào ScriptableObjects
        /// </summary>
        public static void SyncAgentConfigurationsToScriptableObjects(List<AgentConfigurationData> firestoreData)
        {
            if (firestoreData == null || firestoreData.Count == 0)
            {
                Debug.LogWarning("[GameDataSyncService] No agent data to sync");
                return;
            }

#if UNITY_EDITOR
            string folderPath = "Assets/Resources/AgentData";
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                {
                    AssetDatabase.CreateFolder("Assets", "Resources");
                }
                AssetDatabase.CreateFolder("Assets/Resources", "AgentData");
            }

            int syncedCount = 0;
            int createdCount = 0;
            int updatedCount = 0;

            foreach (var data in firestoreData)
            {
                try
                {
                    AgentType agentType = (AgentType)data.type;
                    string assetPath = $"{folderPath}/AgentConfiguration_{agentType}.asset";

                    AgentConfiguration scriptableObject = AssetDatabase.LoadAssetAtPath<AgentConfiguration>(assetPath);

                    if (scriptableObject == null)
                    {
                        scriptableObject = ScriptableObject.CreateInstance<AgentConfiguration>();
                        AssetDatabase.CreateAsset(scriptableObject, assetPath);
                        createdCount++;
                        Debug.Log($"[GameDataSyncService] Created ScriptableObject: {assetPath}");
                    }
                    else
                    {
                        updatedCount++;
                        Debug.Log($"[GameDataSyncService] Updating ScriptableObject: {assetPath}");
                    }

                    scriptableObject.agentType = agentType;
                    scriptableObject.agentName = data.agentName ?? "";
                    scriptableObject.agentDescription = data.agentDescription ?? "";
                    // Note: agentPrefab không được sync vì cần reference từ Unity (phải set manual trong Editor)

                    EditorUtility.SetDirty(scriptableObject);
                    syncedCount++;
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[GameDataSyncService] Error syncing agent type {data.type}: {ex.Message}");
                    Debug.LogError($"[GameDataSyncService] Exception: {ex}");
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[GameDataSyncService] ✅ Synced {syncedCount} agent ScriptableObjects ({createdCount} created, {updatedCount} updated)");
#else
            Debug.LogWarning("[GameDataSyncService] ScriptableObject sync chỉ hoạt động trong Editor mode.");
#endif
        }

        /// <summary>
        /// Sync LevelList từ Firestore vào ScriptableObject
        /// </summary>
        public static void SyncLevelListToScriptableObject(LevelListData firestoreData)
        {
            if (firestoreData == null || firestoreData.levels == null)
            {
                Debug.LogWarning("[GameDataSyncService] No level list data to sync");
                return;
            }

#if UNITY_EDITOR
            string folderPath = "Assets/Resources";
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }

            string assetPath = $"{folderPath}/LevelList.asset";
            LevelList scriptableObject = AssetDatabase.LoadAssetAtPath<LevelList>(assetPath);

            bool isNew = scriptableObject == null;
            if (scriptableObject == null)
            {
                scriptableObject = ScriptableObject.CreateInstance<LevelList>();
                AssetDatabase.CreateAsset(scriptableObject, assetPath);
            }

            // Convert LevelItemData sang LevelItem
            List<LevelItem> levelItems = new List<LevelItem>();
            foreach (var itemData in firestoreData.levels)
            {
                levelItems.Add(new LevelItem
                {
                    id = itemData.id ?? "",
                    name = itemData.name ?? "",
                    description = itemData.description ?? "",
                    sceneName = itemData.sceneName ?? ""
                });
            }

            scriptableObject.levels = levelItems.ToArray();
            EditorUtility.SetDirty(scriptableObject);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[GameDataSyncService] ✅ Synced LevelList ScriptableObject with {levelItems.Count} levels ({(isNew ? "Created" : "Updated")})");
#else
            Debug.LogWarning("[GameDataSyncService] ScriptableObject sync chỉ hoạt động trong Editor mode.");
#endif
        }
    }
}

