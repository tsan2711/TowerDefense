using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TowerDefense.Agents.Data;
using Services.Data;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Services.Firestore
{
    /// <summary>
    /// Runtime service to sync AgentConfiguration data from Firestore to Resources ScriptableObjects
    /// This allows backend to configure agent data and sync it to local Resources
    /// 
    /// NOTE: ScriptableObject creation/update only works in Editor/Play mode.
    /// In built games, ScriptableObjects must be pre-generated and included in Resources folder.
    /// This sync is useful for:
    /// - Development: Automatically sync backend configs to Resources
    /// - Testing: Update agent configs without manual ScriptableObject creation
    /// </summary>
    public static class AgentConfigurationResourceSync
    {
        private const string RESOURCES_PATH = "Resources/Agents";
        private const string FULL_RESOURCES_PATH = "Assets/" + RESOURCES_PATH;

        /// <summary>
        /// Sync AgentConfigurationData from Firestore to Resources ScriptableObjects
        /// Creates or updates ScriptableObjects in Resources/Agents folder
        /// </summary>
        /// <param name="agentConfigurations">List of AgentConfigurationData from Firestore</param>
        /// <returns>Number of ScriptableObjects created/updated</returns>
        public static int SyncToResources(List<AgentConfigurationData> agentConfigurations)
        {
            if (agentConfigurations == null || agentConfigurations.Count == 0)
            {
                Debug.LogWarning("[AgentConfigurationResourceSync] No agent configurations to sync");
                return 0;
            }

#if UNITY_EDITOR
            return SyncToResourcesEditor(agentConfigurations);
#else
            // Runtime: ScriptableObjects can be loaded but not created
            // We can only work with existing ones or use Resources.Load
            Debug.Log("[AgentConfigurationResourceSync] Runtime mode - ScriptableObjects can only be loaded, not created");
            return LoadFromResources(agentConfigurations);
#endif
        }

#if UNITY_EDITOR
        /// <summary>
        /// Sync to Resources in Editor mode (can create/update ScriptableObjects)
        /// </summary>
        private static int SyncToResourcesEditor(List<AgentConfigurationData> agentConfigurations)
        {
            EnsureResourcesFolderExists();

            int createdCount = 0;
            int updatedCount = 0;

            foreach (var configData in agentConfigurations)
            {
                try
                {
                    AgentType agentType = (AgentType)configData.type;
                    string fileName = agentType.ToString() + ".asset";
                    string assetPath = FULL_RESOURCES_PATH + "/" + fileName;

                    // Load existing asset or create new
                    AgentConfiguration scriptableObject = AssetDatabase.LoadAssetAtPath<AgentConfiguration>(assetPath);

                    if (scriptableObject == null)
                    {
                        // Create new ScriptableObject
                        scriptableObject = ScriptableObject.CreateInstance<AgentConfiguration>();
                        scriptableObject.agentType = agentType;
                        scriptableObject.agentName = configData.agentName ?? agentType.ToString();
                        scriptableObject.agentDescription = configData.agentDescription ?? $"Description for {agentType}";

                        AssetDatabase.CreateAsset(scriptableObject, assetPath);
                        AssetDatabase.SaveAssets();
                        createdCount++;
                        Debug.Log($"[AgentConfigurationResourceSync] Created: {fileName}");
                    }
                    else
                    {
                        // Update existing ScriptableObject with Firestore data
                        scriptableObject.agentType = agentType;
                        scriptableObject.agentName = configData.agentName ?? agentType.ToString();
                        scriptableObject.agentDescription = configData.agentDescription ?? $"Description for {agentType}";

                        EditorUtility.SetDirty(scriptableObject);
                        AssetDatabase.SaveAssets();
                        updatedCount++;
                        Debug.Log($"[AgentConfigurationResourceSync] Updated: {fileName}");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[AgentConfigurationResourceSync] Error syncing agent type {configData.type}: {e.Message}");
                }
            }

            AssetDatabase.Refresh();

            Debug.Log($"[AgentConfigurationResourceSync] ✅ Sync complete! Created: {createdCount}, Updated: {updatedCount}, Total: {createdCount + updatedCount}");
            return createdCount + updatedCount;
        }

        /// <summary>
        /// Ensure Resources/Agents folder structure exists in Editor
        /// </summary>
        private static void EnsureResourcesFolderExists()
        {
            // Create Resources folder if it doesn't exist
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
                Debug.Log("[AgentConfigurationResourceSync] Created folder: Assets/Resources");
            }

            // Create Agents folder inside Resources if it doesn't exist
            if (!AssetDatabase.IsValidFolder(FULL_RESOURCES_PATH))
            {
                AssetDatabase.CreateFolder("Assets/Resources", "Agents");
                Debug.Log($"[AgentConfigurationResourceSync] Created folder: {FULL_RESOURCES_PATH}");
            }
        }
#endif

        /// <summary>
        /// Load AgentConfiguration ScriptableObjects from Resources at runtime
        /// </summary>
        private static int LoadFromResources(List<AgentConfigurationData> agentConfigurations)
        {
            int loadedCount = 0;

            foreach (var configData in agentConfigurations)
            {
                try
                {
                    AgentType agentType = (AgentType)configData.type;
                    string resourcePath = "Agents/" + agentType.ToString();
                    
                    AgentConfiguration scriptableObject = Resources.Load<AgentConfiguration>(resourcePath);
                    
                    if (scriptableObject != null)
                    {
                        // Update runtime data from ScriptableObject (read-only in runtime)
                        Debug.Log($"[AgentConfigurationResourceSync] Loaded from Resources: {agentType}");
                        loadedCount++;
                    }
                    else
                    {
                        Debug.LogWarning($"[AgentConfigurationResourceSync] ScriptableObject not found in Resources: {resourcePath}");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[AgentConfigurationResourceSync] Error loading agent type {configData.type}: {e.Message}");
                }
            }

            Debug.Log($"[AgentConfigurationResourceSync] Loaded {loadedCount} ScriptableObjects from Resources");
            return loadedCount;
        }

        /// <summary>
        /// Load all AgentConfiguration ScriptableObjects from Resources
        /// </summary>
        public static AgentConfiguration[] LoadAllFromResources()
        {
            AgentConfiguration[] configs = Resources.LoadAll<AgentConfiguration>("Agents");
            Debug.Log($"[AgentConfigurationResourceSync] Loaded {configs.Length} AgentConfigurations from Resources");
            return configs;
        }

        /// <summary>
        /// Get AgentConfiguration by AgentType from Resources
        /// </summary>
        public static AgentConfiguration GetFromResources(AgentType agentType)
        {
            string resourcePath = "Agents/" + agentType.ToString();
            AgentConfiguration config = Resources.Load<AgentConfiguration>(resourcePath);
            
            if (config == null)
            {
                Debug.LogWarning($"[AgentConfigurationResourceSync] AgentConfiguration not found: {resourcePath}");
            }
            
            return config;
        }

        /// <summary>
        /// Get AgentConfiguration by AgentType enum value from Resources
        /// </summary>
        public static AgentConfiguration GetFromResources(int agentTypeValue)
        {
            AgentType agentType = (AgentType)agentTypeValue;
            return GetFromResources(agentType);
        }
    }
}

