using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
#if FIREBASE_FIRESTORE_AVAILABLE
using Firebase.Firestore;
#endif
using Services.Core;
using Services.Data;
using Services.Config;
using Services.Managers;

namespace Services.Firestore
{
    /// <summary>
    /// Microservice for managing Agent Configuration data
    /// Handles only Agent Configuration domain operations
    /// </summary>
    public class AgentConfigurationService : FirestoreServiceBase, IAgentConfigurationService
    {
        private const string DEFAULT_COLLECTION_AGENT_CONFIGURATIONS = "AgentConfigurations";
        private string COLLECTION_AGENT_CONFIGURATIONS => 
            FirebaseConfigManager.Instance?.GetAgentConfigurationsCollection() ?? DEFAULT_COLLECTION_AGENT_CONFIGURATIONS;

        private List<AgentConfigurationData> cachedAgentConfigurations;

        public event Action<List<AgentConfigurationData>> OnAgentConfigurationsLoaded;

        protected override string GetServiceName() => "AgentConfigurationService";

        public async Task<List<AgentConfigurationData>> LoadAgentConfigurationsAsync()
        {
#if !FIREBASE_FIRESTORE_AVAILABLE
            Debug.LogError($"[{GetServiceName()}] Firebase Firestore package chưa được cài đặt!");
            return new List<AgentConfigurationData>();
#else
            if (!isInitialized || firestore == null)
            {
                Debug.LogError($"[{GetServiceName()}] Service not initialized");
                return new List<AgentConfigurationData>();
            }

            try
            {
                QuerySnapshot snapshot = await firestore.Collection(COLLECTION_AGENT_CONFIGURATIONS).GetSnapshotAsync();

                List<AgentConfigurationData> configurations = new List<AgentConfigurationData>();
                foreach (DocumentSnapshot document in snapshot.Documents)
                {
                    Dictionary<string, object> data = document.ToDictionary();
                    AgentConfigurationData config = ParseAgentConfiguration(data);
                    if (config != null)
                    {
                        // Only add if enum value matches client enum (safety check)
                        if (DefaultGameData.IsValidAgentType(config.type))
                        {
                            configurations.Add(config);
                        }
                        else
                        {
                            Debug.LogWarning($"[{GetServiceName()}] Skipping AgentConfiguration with invalid enum type: {config.type} (not in client AgentType enum)");
                        }
                    }
                }

                cachedAgentConfigurations = configurations;
                OnAgentConfigurationsLoaded?.Invoke(configurations);
                Debug.Log($"[{GetServiceName()}] Loaded {configurations.Count} AgentConfigurations");
                return configurations;
            }
            catch (Exception e)
            {
                string errorMsg = e.Message;
                if (errorMsg.Contains("permission") || errorMsg.Contains("Permission"))
                {
                    Debug.LogError($"[{GetServiceName()}] Permission denied when loading AgentConfigurations. " +
                                 $"Vui lòng cấu hình Firestore Rules trong Firebase Console. " +
                                 $"Xem hướng dẫn: FIRESTORE_RULES_SETUP.md");
                }
                else
                {
                    Debug.LogError($"[{GetServiceName()}] Error loading AgentConfigurations: {errorMsg}");
                }
                Debug.LogError($"[{GetServiceName()}] Full exception: {e}");
                return new List<AgentConfigurationData>();
            }
#endif
        }

        public async Task<List<AgentConfigurationData>> LoadAgentConfigurationsByTypeAsync(int type)
        {
#if !FIREBASE_FIRESTORE_AVAILABLE
            Debug.LogError($"[{GetServiceName()}] Firebase Firestore package chưa được cài đặt!");
            return new List<AgentConfigurationData>();
#else
            if (!isInitialized || firestore == null)
            {
                Debug.LogError($"[{GetServiceName()}] Service not initialized");
                return new List<AgentConfigurationData>();
            }

            try
            {
                Query query = firestore.Collection(COLLECTION_AGENT_CONFIGURATIONS)
                    .WhereEqualTo("type", type);
                QuerySnapshot snapshot = await query.GetSnapshotAsync();

                List<AgentConfigurationData> configurations = new List<AgentConfigurationData>();
                foreach (DocumentSnapshot document in snapshot.Documents)
                {
                    Dictionary<string, object> data = document.ToDictionary();
                    AgentConfigurationData config = ParseAgentConfiguration(data);
                    if (config != null)
                    {
                        // Only add if enum value matches client enum (safety check)
                        if (DefaultGameData.IsValidAgentType(config.type))
                        {
                            configurations.Add(config);
                        }
                        else
                        {
                            Debug.LogWarning($"[{GetServiceName()}] Skipping AgentConfiguration with invalid enum type: {config.type} (not in client AgentType enum)");
                        }
                    }
                }

                Debug.Log($"[{GetServiceName()}] Loaded {configurations.Count} AgentConfigurations with type {type}");
                return configurations;
            }
            catch (Exception e)
            {
                Debug.LogError($"[{GetServiceName()}] Error loading AgentConfigurations by type: {e.Message}");
                return new List<AgentConfigurationData>();
            }
#endif
        }

        public List<AgentConfigurationData> GetCachedAgentConfigurations()
        {
            return cachedAgentConfigurations ?? new List<AgentConfigurationData>();
        }

        public async Task<bool> InitializeCollectionIfEmptyAsync()
        {
#if !FIREBASE_FIRESTORE_AVAILABLE
            Debug.LogError($"[{GetServiceName()}] Firebase Firestore package chưa được cài đặt!");
            return false;
#else
            if (!isInitialized || firestore == null)
            {
                Debug.LogError($"[{GetServiceName()}] Service not initialized");
                return false;
            }

            try
            {
                // Check if collection is empty
                QuerySnapshot existingSnapshot = await firestore.Collection(COLLECTION_AGENT_CONFIGURATIONS)
                    .Limit(1)
                    .GetSnapshotAsync();
                
                if (existingSnapshot.Count > 0)
                {
                    Debug.Log($"[{GetServiceName()}] Collection {COLLECTION_AGENT_CONFIGURATIONS} is not empty, skipping initialization");
                    return true;
                }

                Debug.Log($"[{GetServiceName()}] Collection {COLLECTION_AGENT_CONFIGURATIONS} is empty, initializing...");
                return await InitializeAgentConfigurationsCollectionAsync();
            }
            catch (Exception e)
            {
                Debug.LogError($"[{GetServiceName()}] Error checking/initializing collection: {e.Message}");
                return false;
            }
#endif
        }

#if FIREBASE_FIRESTORE_AVAILABLE
        private async Task<bool> InitializeAgentConfigurationsCollectionAsync()
        {
            try
            {
                // Get default configurations for all enum values
                List<AgentConfigurationData> defaultConfigs = DefaultGameData.GetDefaultAgentConfigurations();
                
                if (defaultConfigs == null || defaultConfigs.Count == 0)
                {
                    Debug.LogError($"[{GetServiceName()}] GetDefaultAgentConfigurations returned empty list!");
                    return false;
                }
                
                Debug.Log($"[{GetServiceName()}] Preparing to initialize {defaultConfigs.Count} agent types");
                
                // Check existing documents
                QuerySnapshot existingSnapshot = await firestore.Collection(COLLECTION_AGENT_CONFIGURATIONS)
                    .GetSnapshotAsync();
                
                HashSet<int> existingTypes = new HashSet<int>();
                foreach (DocumentSnapshot doc in existingSnapshot.Documents)
                {
                    if (doc.TryGetValue("type", out object typeObj) && typeObj != null)
                    {
                        try
                        {
                            int type = Convert.ToInt32(typeObj);
                            existingTypes.Add(type);
                        }
                        catch { }
                    }
                }
                
                Debug.Log($"[{GetServiceName()}] Found {existingTypes.Count} existing agent type documents in Firestore");
                
                int createdCount = 0;
                int skippedCount = 0;
                
                // Create documents for all enum values (skip if already exist to preserve existing data)
                foreach (var config in defaultConfigs)
                {
                    // Validate enum value exists in client
                    if (!DefaultGameData.IsValidAgentType(config.type))
                    {
                        Debug.LogWarning($"[{GetServiceName()}] Skipping invalid AgentType: {config.type}");
                        skippedCount++;
                        continue;
                    }
                    
                    // Use padded document ID (2 digits) to ensure correct sorting: "00", "01", ..., "07"
                    string docId = config.type.ToString("D2"); // "D2" = decimal format with 2 digits padding
                    DocumentReference docRef = firestore.Collection(COLLECTION_AGENT_CONFIGURATIONS).Document(docId);
                    
                    Dictionary<string, object> data = new Dictionary<string, object>
                    {
                        { "type", config.type },
                        { "agentName", config.agentName ?? "" },
                        { "agentDescription", config.agentDescription ?? "" },
                        { "updatedAt", Timestamp.GetCurrentTimestamp() }
                    };
                    
                    if (!existingTypes.Contains(config.type))
                    {
                        // Create new document
                        data["createdAt"] = Timestamp.GetCurrentTimestamp();
                        await docRef.SetAsync(data);
                        createdCount++;
                        Debug.Log($"[{GetServiceName()}] Created agent type {config.type} ({docId}) in Firestore");
                    }
                    else
                    {
                        // ✅ QUAN TRỌNG: Không update document đã tồn tại để preserve data từ backend
                        skippedCount++;
                        Debug.Log($"[{GetServiceName()}] Agent type {config.type} ({docId}) already exists, skipping to preserve existing data");
                    }
                }
                
                Debug.Log($"[{GetServiceName()}] ✅ Initialized {COLLECTION_AGENT_CONFIGURATIONS}: {createdCount} created, {skippedCount} skipped (total: {defaultConfigs.Count})");
                return true;
            }
            catch (Exception e)
            {
                string errorMsg = e.Message;
                if (errorMsg.Contains("permission") || errorMsg.Contains("Permission"))
                {
                    Debug.LogError($"[{GetServiceName()}] Permission denied when initializing {COLLECTION_AGENT_CONFIGURATIONS}. " +
                                 $"Vui lòng cấu hình Firestore Rules trong Firebase Console. " +
                                 $"Xem hướng dẫn: FIRESTORE_RULES_SETUP.md");
                }
                else
                {
                    Debug.LogError($"[{GetServiceName()}] Error initializing {COLLECTION_AGENT_CONFIGURATIONS}: {errorMsg}");
                }
                return false;
            }
        }

        private AgentConfigurationData ParseAgentConfiguration(Dictionary<string, object> data)
        {
            try
            {
                AgentConfigurationData config = new AgentConfigurationData();
                
                if (data.ContainsKey("type") && data["type"] != null)
                {
                    config.type = Convert.ToInt32(data["type"]);
                }
                
                if (data.ContainsKey("agentName") && data["agentName"] != null)
                {
                    config.agentName = data["agentName"].ToString();
                }
                
                if (data.ContainsKey("agentDescription") && data["agentDescription"] != null)
                {
                    config.agentDescription = data["agentDescription"].ToString();
                }

                return config;
            }
            catch (Exception e)
            {
                Debug.LogError($"[{GetServiceName()}] Error parsing AgentConfiguration: {e.Message}");
                return null;
            }
        }
#endif

        public override void Shutdown()
        {
            base.Shutdown();
            cachedAgentConfigurations = null;
        }
    }
}

