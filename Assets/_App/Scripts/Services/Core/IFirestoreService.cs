using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Services.Data;

namespace Services.Core
{
    /// <summary>
    /// Firestore service interface for handling data synchronization from Firebase Firestore
    /// Loads configuration data from backend after successful authentication
    /// </summary>
    public interface IFirestoreService : IService
    {
        /// <summary>
        /// Event fired when AgentConfiguration data is loaded
        /// </summary>
        event Action<List<AgentConfigurationData>> OnAgentConfigurationsLoaded;

        /// <summary>
        /// Event fired when TowerLevelData is loaded
        /// </summary>
        event Action<List<TowerLevelDataData>> OnTowerLevelDataLoaded;

        /// <summary>
        /// Event fired when LevelList is loaded
        /// </summary>
        event Action<LevelListData> OnLevelListLoaded;

        /// <summary>
        /// Event fired when LevelLibraryConfigs are loaded
        /// </summary>
        event Action<List<LevelLibraryConfigData>> OnLevelLibraryConfigsLoaded;

        /// <summary>
        /// Event fired when all configuration data is loaded
        /// </summary>
        event Action OnAllConfigDataLoaded;

        /// <summary>
        /// Check if all configuration data has been loaded
        /// </summary>
        bool IsConfigDataLoaded { get; }

        /// <summary>
        /// Load all AgentConfiguration data from Firestore
        /// </summary>
        Task<List<AgentConfigurationData>> LoadAgentConfigurationsAsync();

        /// <summary>
        /// Load AgentConfiguration data filtered by type
        /// </summary>
        Task<List<AgentConfigurationData>> LoadAgentConfigurationsByTypeAsync(int type);

        /// <summary>
        /// Load all TowerLevelData from Firestore
        /// </summary>
        Task<List<TowerLevelDataData>> LoadTowerLevelDataAsync();

        /// <summary>
        /// Load TowerLevelData filtered by type
        /// </summary>
        Task<List<TowerLevelDataData>> LoadTowerLevelDataByTypeAsync(int type);

        /// <summary>
        /// Load LevelList from Firestore
        /// </summary>
        Task<LevelListData> LoadLevelListAsync();

        /// <summary>
        /// Load all configuration data (AgentConfigurations, TowerLevelData, LevelList)
        /// This should be called after successful authentication
        /// </summary>
        Task LoadAllConfigDataAsync();

        /// <summary>
        /// Get cached AgentConfiguration data
        /// </summary>
        List<AgentConfigurationData> GetCachedAgentConfigurations();

        /// <summary>
        /// Get cached TowerLevelData
        /// </summary>
        List<TowerLevelDataData> GetCachedTowerLevelData();

        /// <summary>
        /// Get cached LevelList
        /// </summary>
        LevelListData GetCachedLevelList();

        /// <summary>
        /// Load all LevelLibraryConfig from Firestore
        /// </summary>
        Task<List<LevelLibraryConfigData>> LoadLevelLibraryConfigsAsync();

        /// <summary>
        /// Load LevelLibraryConfig filtered by levelId
        /// </summary>
        Task<LevelLibraryConfigData> LoadLevelLibraryConfigByLevelIdAsync(string levelId);

        /// <summary>
        /// Load LevelLibraryConfig filtered by type
        /// </summary>
        Task<LevelLibraryConfigData> LoadLevelLibraryConfigByTypeAsync(int type);

        /// <summary>
        /// Get cached LevelLibraryConfigs
        /// </summary>
        List<LevelLibraryConfigData> GetCachedLevelLibraryConfigs();

        /// <summary>
        /// Save or update user data in Firestore users collection
        /// Collection will be created automatically if it doesn't exist
        /// </summary>
        /// <param name="userInfo">User information to save</param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> SaveUserDataAsync(UserInfo userInfo);

        /// <summary>
        /// Initialize collections (AgentConfigurations, TowerLevelData, LevelList) with placeholder data if they don't exist
        /// Collections will be created automatically if they don't exist
        /// </summary>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> InitializeCollectionsIfEmptyAsync();
    }
}

