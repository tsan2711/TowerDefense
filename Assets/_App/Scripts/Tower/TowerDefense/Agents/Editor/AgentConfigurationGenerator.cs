using System;
using UnityEditor;
using UnityEngine;
using TowerDefense.Agents.Data;
using Services.Firestore;

namespace TowerDefense.Agents.Editor
{
    /// <summary>
    /// Editor tool to generate AgentConfiguration ScriptableObjects for all AgentType enum values
    /// Creates ScriptableObjects in Resources/Agents folder
    /// </summary>
    public class AgentConfigurationGenerator : EditorWindow
    {
        private const string RESOURCES_PATH = "Assets/Resources";
        private const string AGENTS_FOLDER = "Agents";
        private const string FULL_RESOURCES_PATH = RESOURCES_PATH + "/" + AGENTS_FOLDER;

        [MenuItem("TowerDefense/Generate Agent Configurations")]
        public static void ShowWindow()
        {
            GetWindow<AgentConfigurationGenerator>("Agent Configuration Generator");
        }

        [MenuItem("TowerDefense/Generate Agent Configurations (Quick)")]
        public static void GenerateAgentConfigurationsQuick()
        {
            GenerateAllAgentConfigurations();
        }

        void OnGUI()
        {
            GUILayout.Label("Agent Configuration Generator", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox(
                "This tool will generate ScriptableObject AgentConfiguration files for all AgentType enum values.\n" +
                "Files will be created in: " + FULL_RESOURCES_PATH,
                MessageType.Info);

            EditorGUILayout.Space();

            if (GUILayout.Button("Generate All Agent Configurations", GUILayout.Height(30)))
            {
                GenerateAllAgentConfigurations();
            }

            EditorGUILayout.Space();

            if (GUILayout.Button("Open Resources/Agents Folder", GUILayout.Height(25)))
            {
                EnsureResourcesFolderExists();
                EditorUtility.RevealInFinder(FULL_RESOURCES_PATH);
            }
        }

        /// <summary>
        /// Generate ScriptableObjects for all AgentType enum values
        /// </summary>
        public static void GenerateAllAgentConfigurations()
        {
            try
            {
                // Ensure Resources/Agents folder exists
                EnsureResourcesFolderExists();

                // Get default configurations from DefaultGameData
                var defaultConfigs = DefaultGameData.GetDefaultAgentConfigurations();
                
                int createdCount = 0;
                int updatedCount = 0;

                // Get all AgentType enum values
                var enumValues = Enum.GetValues(typeof(AgentType));

                foreach (AgentType agentType in enumValues)
                {
                    // Find matching default config
                    var defaultConfig = defaultConfigs?.Find(c => c.type == (int)agentType);

                    string fileName = agentType.ToString() + ".asset";
                    string assetPath = FULL_RESOURCES_PATH + "/" + fileName;

                    // Check if asset already exists
                    AgentConfiguration existingAsset = AssetDatabase.LoadAssetAtPath<AgentConfiguration>(assetPath);

                    if (existingAsset != null)
                    {
                        // Update existing asset with default data
                        existingAsset.agentType = agentType;
                        
                        if (defaultConfig != null)
                        {
                            existingAsset.agentName = defaultConfig.agentName;
                            existingAsset.agentDescription = defaultConfig.agentDescription;
                        }
                        else
                        {
                            existingAsset.agentName = agentType.ToString();
                            existingAsset.agentDescription = $"Default description for {agentType}";
                        }

                        EditorUtility.SetDirty(existingAsset);
                        updatedCount++;
                        Debug.Log($"[AgentConfigGenerator] Updated existing: {fileName}");
                    }
                    else
                    {
                        // Create new ScriptableObject
                        AgentConfiguration config = ScriptableObject.CreateInstance<AgentConfiguration>();
                        config.agentType = agentType;

                        if (defaultConfig != null)
                        {
                            config.agentName = defaultConfig.agentName;
                            config.agentDescription = defaultConfig.agentDescription;
                        }
                        else
                        {
                            config.agentName = agentType.ToString();
                            config.agentDescription = $"Default description for {agentType}";
                        }

                        AssetDatabase.CreateAsset(config, assetPath);
                        createdCount++;
                        Debug.Log($"[AgentConfigGenerator] Created: {fileName}");
                    }
                }

                // Save all assets
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                // Show result
                string message = $"Agent Configuration Generation Complete!\n\n" +
                               $"Created: {createdCount}\n" +
                               $"Updated: {updatedCount}\n" +
                               $"Total: {createdCount + updatedCount}\n\n" +
                               $"Files saved to: {FULL_RESOURCES_PATH}";

                EditorUtility.DisplayDialog("Generation Complete", message, "OK");

                Debug.Log($"[AgentConfigGenerator] ✅ Complete! Created: {createdCount}, Updated: {updatedCount}");
            }
            catch (Exception e)
            {
                string errorMsg = $"Error generating Agent Configurations: {e.Message}";
                EditorUtility.DisplayDialog("Error", errorMsg, "OK");
                Debug.LogError($"[AgentConfigGenerator] {errorMsg}\n{e}");
            }
        }

        /// <summary>
        /// Ensure Resources/Agents folder structure exists
        /// </summary>
        private static void EnsureResourcesFolderExists()
        {
            // Create Resources folder if it doesn't exist
            if (!AssetDatabase.IsValidFolder(RESOURCES_PATH))
            {
                string parentFolder = "Assets";
                string folderName = "Resources";
                AssetDatabase.CreateFolder(parentFolder, folderName);
                Debug.Log($"[AgentConfigGenerator] Created folder: {RESOURCES_PATH}");
            }

            // Create Agents folder inside Resources if it doesn't exist
            if (!AssetDatabase.IsValidFolder(FULL_RESOURCES_PATH))
            {
                AssetDatabase.CreateFolder(RESOURCES_PATH, AGENTS_FOLDER);
                Debug.Log($"[AgentConfigGenerator] Created folder: {FULL_RESOURCES_PATH}");
            }
        }

        /// <summary>
        /// Load all generated AgentConfigurations from Resources
        /// </summary>
        public static AgentConfiguration[] LoadAllAgentConfigurations()
        {
            EnsureResourcesFolderExists();
            
            string[] guids = AssetDatabase.FindAssets("t:AgentConfiguration", new[] { FULL_RESOURCES_PATH });
            AgentConfiguration[] configs = new AgentConfiguration[guids.Length];

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                configs[i] = AssetDatabase.LoadAssetAtPath<AgentConfiguration>(path);
            }

            return configs;
        }

        /// <summary>
        /// Get AgentConfiguration by AgentType from Resources
        /// </summary>
        public static AgentConfiguration GetAgentConfiguration(AgentType agentType)
        {
            string assetPath = FULL_RESOURCES_PATH + "/" + agentType.ToString() + ".asset";
            return AssetDatabase.LoadAssetAtPath<AgentConfiguration>(assetPath);
        }
    }
}

